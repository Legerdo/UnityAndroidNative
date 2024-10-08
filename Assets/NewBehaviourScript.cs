using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] Text testText;

    // 안드로이드 플러그인 인스턴스 캐싱용
    private AndroidJavaObject activityContext = null;
    private AndroidJavaClass javaClass = null;
    private AndroidJavaObject javaClassInstance = null;

    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // UnityPlayer의 currentActivity를 캐싱
        using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
        }

        // 네이티브 클래스 인스턴스화
        using (javaClass = new AndroidJavaClass("com.legeodo.aosextension.MyNativeClass"))
        {
            if (javaClass != null)
            {
                // 인스턴스 생성 및 호출 - 인스턴스를 통해 Android 네이티브 객체에 접근해야만 Unity와의 통신에서 필요한 상태값(예: Context)이 유지되고, 올바르게 동작함.
                javaClassInstance = javaClass.CallStatic<AndroidJavaObject>("instance");

                // context 전달
                javaClassInstance.Call("setContext", activityContext);
            }
        }

        // 네이티브 메서드 호출
        callJava();
        #else
        Debug.Log("Android 네이티브 기능은 에디터에서 사용할 수 없습니다.");
        #endif
    }

    void callJava()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Unity 메시지 전송용 인자
        System.Object[] objs = new System.Object[2];
        objs[0] = "TestObject"; // 유니티에서 받을 오브젝트 이름
        objs[1] = "OutPutLog"; // 유니티에서 호출될 함수 이름

        // 네이티브 클래스의 TestLog 호출
        javaClassInstance.Call("TestLog", objs);

        // 네이티브 클래스의 ShowToast 호출
        javaClassInstance.Call("ShowToast");
        #else
        Debug.Log("callJava 메서드는 에디터에서 실행되지 않습니다.");
        #endif
    }

    // 안드로이드에서 호출되는 로그 출력 함수
    void OutPutLog(string msg)
    {
        testText.text = msg;
        Debug.Log(msg);
    }
}
