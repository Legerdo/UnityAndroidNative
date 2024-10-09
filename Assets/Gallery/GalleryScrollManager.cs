using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine.Networking;

[RequireComponent(typeof(UnityEngine.UI.LoopScrollRect))]
[DisallowMultipleComponent]
public class GalleryScrollManager : SingletonMonoBehaviour<GalleryScrollManager>, LoopScrollPrefabSource, LoopScrollDataSource
{
    public GameObject itemPrefab;
    public GalleryHelper galleryHelper; // GalleryHelper 참조

    private List<string> imagePaths = new List<string>();

    // In-memory 캐싱을 위한 옵션과 캐시 딕셔너리
    [SerializeField]
    private bool useInMemoryCache = true;
    private Dictionary<int, Texture2D> inMemoryCache = new Dictionary<int, Texture2D>();

    // Implement your own Cache Pool here. The following is just for example.
    Stack<Transform> pool = new Stack<Transform>();

    // 최대 동시 로딩 개수 설정
    [SerializeField]
    private int maxConcurrentLoads = 3;

    // Semaphore to control concurrent image loading
    private SemaphoreSlim loadSemaphore;

    // 이미지 로딩 카운터 및 임계값
    [SerializeField]
    private int gcCallThreshold = 10; // 예: 10회마다 GC 호출
    private int imageLoadCounter = 0;

    override protected void Awake()
    {
        base.Awake();        

        // SemaphoreSlim 초기화
        loadSemaphore = new SemaphoreSlim(maxConcurrentLoads, maxConcurrentLoads);
    }

    void Start()
    {
        // GalleryHelper의 OnImagesReceived 이벤트에 콜백 연결
        if (galleryHelper != null)
        {
            galleryHelper.OnImagesReceived += OnImagesReceived;
            galleryHelper.GetGalleryImages(); // 이미지 요청 시작
        }
        else
        {
            Debug.LogError("GalleryHelper가 할당되지 않았습니다.");
        }
    }

    public GameObject GetObject(int index)
    {
        if (pool.Count == 0)
        {
            return Instantiate(itemPrefab);
        }
        Transform candidate = pool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // ScrollCellReturn을 호출하여 비동기 작업 취소
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        pool.Push(trans);
    }

    public void ProvideData(Transform transform, int idx)
    {
        transform.SendMessage("ScrollCellIndex", idx);
    }

    private void OnImagesReceived(List<string> paths)
    {
        imagePaths = paths;

        Debug.Log("Total Size = " + imagePaths.Count);

        var ls = GetComponent<LoopScrollRect>();
        ls.prefabSource = this;
        ls.dataSource = this;
        ls.totalCount = imagePaths.Count;
        ls.RefillCells();
    }

    // 이미지 경로를 외부에서 접근할 수 있도록
    public string GetImagePath(int index)
    {
        if (index >= 0 && index < imagePaths.Count)
        {
            return imagePaths[index];
        }
        return null;
    }

    public async UniTask<Texture2D> LoadImageAsync(string imagePath, int index, int maxWidth = 512, int maxHeight = 512, CancellationToken cancellationToken = default)
    {
        // 동시성 제어를 위한 세마포어 대기
        await loadSemaphore.WaitAsync(cancellationToken);
        try
        {
            // 메모리 캐시에 존재하는지 확인
            if (useInMemoryCache && inMemoryCache.ContainsKey(index))
            {
                IncrementImageLoadCounter();
                return inMemoryCache[index];
            }

            Texture2D texture = null;

            // UnityWebRequestTexture를 사용하여 이미지를 비동기로 로딩
            texture = await LoadTextureAsync(imagePath, cancellationToken);

            if (texture != null)
            {
                // 필요하다면 텍스처 크기 조정
                Texture2D resizedTexture = ResizeTexture(texture, maxWidth, maxHeight);

                if (useInMemoryCache)
                {
                    inMemoryCache[index] = resizedTexture;
                }

                IncrementImageLoadCounter();
                return resizedTexture;
            }
            else
            {
                Debug.LogError($"Failed to load texture from {imagePath}");
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"Image loading canceled: {imagePath}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during image loading: {ex.Message}");
            return null;
        }
        finally
        {
            // 세마포어 해제하여 다음 이미지 로드 허용
            loadSemaphore.Release();
        }
    }

    private async UniTask<Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + path))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        return null;
                    }
                    await UniTask.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    return texture;
                }
                else
                {
                    Debug.LogError($"Failed to load texture from {path}: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during LoadTextureAsync: {ex.Message}");
            return null;
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        RenderTexture.active = rt;

        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    // 메모리 캐시에서 텍스처 제거
    public void RemoveTextureFromCache(int index)
    {
        if (useInMemoryCache && inMemoryCache.ContainsKey(index))
        {
            inMemoryCache.Remove(index);
            // 이미지 언로드 시 카운터를 증가시킬 수도 있습니다.
            // IncrementImageLoadCounter(); // 필요에 따라 주석 해제
        }
    }

    // 이미지 로딩 카운터 증가 및 GC 호출 체크
    private void IncrementImageLoadCounter()
    {
        imageLoadCounter++;
        if (imageLoadCounter >= gcCallThreshold)
        {
            TriggerGarbageCollection();
            imageLoadCounter = 0;
        }
    }

    // 가비지 컬렉션 및 언로드 수행
    private async void TriggerGarbageCollection()
    {
        // 비동기로 Resources.UnloadUnusedAssets 호출
        await Resources.UnloadUnusedAssets();

        // 가비지 컬렉션 강제 호출
        System.GC.Collect();

        Debug.Log("Garbage Collection and Resources.UnloadUnusedAssets called.");
    }
}
