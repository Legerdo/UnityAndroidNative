using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;

public class GalleryScrollCell : MonoBehaviour
{
    public RawImage image;
    public GameObject loadingObject;

    private CancellationTokenSource cancellationTokenSource;
    private int currentIndex; // 현재 셀의 인덱스 저장

    private string imagePath = "";

    // ScrollCellIndex 메서드를 통해 셀의 데이터를 설정
    public void ScrollCellIndex(int idx)
    {
        currentIndex = idx; // 인덱스 저장

        // 이전 작업이 있으면 취소
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        imagePath = GalleryScrollManager.Instance.GetImagePath(idx);

        gameObject.name = idx.ToString();

        if (!string.IsNullOrEmpty(imagePath))
        {
            loadingObject?.SetActive(true);
            LoadAndDisplayImage(idx, imagePath, cancellationTokenSource.Token).Forget(); // 비동기 메서드 실행
        }
        else
        {
            // 이미지 경로가 유효하지 않을 경우 이미지 해제
            image.texture = null;
            loadingObject?.SetActive(false);
        }
    }

    private async UniTaskVoid LoadAndDisplayImage(int index, string imagePath, CancellationToken cancellationToken)
    {
        // imagePath를 이용해 이미지를 로드하고, rawImage에 텍스처 할당
        Texture2D texture = await GalleryScrollManager.Instance.LoadImageAsync(imagePath, index, cancellationToken: cancellationToken);

        if (texture != null)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                image.texture = texture; // RawImage에 텍스처 설정
                loadingObject?.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("Failed to load image.");
        }
    }

    // ScrollCellReturn 메서드를 통해 비동기 작업 취소 및 이미지 해제
    public void ScrollCellReturn()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = null;

        if (image.texture != null)
        {
            Destroy(image.texture);
        }

        image.texture = null;

        imagePath = "";

        loadingObject?.SetActive(true);

        // GalleryScrollManager의 메모리 캐시에서 텍스처 제거
        GalleryScrollManager.Instance.RemoveTextureFromCache(currentIndex);
    }
}
