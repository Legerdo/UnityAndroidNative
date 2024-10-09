using UnityEngine;
using AndroidRuntimePermissionsNamespace; // 플러그인의 네임스페이스

public class PermissionManager : MonoBehaviour
{
    void Start()
    {
        RequestPermissions();
    }

    private void RequestPermissions()
    {
        // 요청할 권한 목록
        string[] permissions = new string[]
        {
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.READ_MEDIA_IMAGES",
            "android.permission.ACCESS_MEDIA_LOCATION",
            "android.permission.MANAGE_EXTERNAL_STORAGE"
        };

        // 권한 요청
        AndroidRuntimePermissions.Permission[] result = AndroidRuntimePermissions.RequestPermissions(permissions);

        // 권한 결과 확인
        foreach (var permission in result)
        {
            if (permission == AndroidRuntimePermissions.Permission.Denied)
            {
                Debug.LogWarning("권한이 거부되었습니다: " + permission);
            }
            else if (permission == AndroidRuntimePermissions.Permission.Granted)
            {
                Debug.Log("권한이 허가되었습니다: " + permission);
            }
            else
            {
                Debug.LogWarning("권한 요청이 영구적으로 거부되었습니다: " + permission);
            }
        }
    }
}
