using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json; // JSON 파싱을 위해 필요
using System.Linq;

public class GalleryHelper : MonoBehaviour
{
    // 이미지 경로 리스트
    private List<string> imagePaths = new List<string>();

    // 이미지 경로를 외부에서 접근할 수 있도록 프로퍼티 추가
    public List<string> ImagePaths => imagePaths;

    // 이벤트 정의
    public System.Action<List<string>> OnImagesReceived;

    // Java에서 호출할 콜백 메소드
    public void OnReceiveImages(string jsonImagePaths)
    {
        Debug.Log("이미지 경로를 받았습니다: " + jsonImagePaths);

        if (string.IsNullOrEmpty(jsonImagePaths))
        {
            Debug.LogError("빈 이미지 경로를 받았습니다.");
            OnImagesReceived?.Invoke(new List<string>());
            return;
        }

        // JSON 배열 파싱
        try
        {
            imagePaths = JsonConvert.DeserializeObject<string[]>(jsonImagePaths).ToList();
            OnImagesReceived?.Invoke(imagePaths);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("JSON 이미지 경로 파싱 실패: " + ex.Message);
            OnImagesReceived?.Invoke(new List<string>());
        }
    }

    // 갤러리 이미지 요청 (Java와의 연동)
    public void GetGalleryImages()
    {
        using (AndroidJavaClass galleryHelper = new AndroidJavaClass("com.legeodo.aosextension.MyNativeClass"))
        {
            galleryHelper.CallStatic("RequestImagesFromGallery");
        }
    }  
}
