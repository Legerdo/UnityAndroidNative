using UnityEngine;
using System;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static readonly Lazy<T> instance = new Lazy<T>(() =>
    {
        T obj = FindObjectOfType<T>();
        if (obj == null)
        {
            GameObject singletonObject = new GameObject();
            obj = singletonObject.AddComponent<T>();
            singletonObject.name = typeof(T).ToString() + " (Singleton)";
            DontDestroyOnLoad(singletonObject);
        }
        return obj;
    });

    public static T Instance
    {
        get
        {
            return instance.Value;
        }
    }

    protected virtual void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
