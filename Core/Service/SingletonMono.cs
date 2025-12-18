using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    private static T instance;
    
    public static T Instance {
        get {
            if (instance == null) instance = FindFirstObjectByType<T>();
            return instance;
        }
        private set => instance = value;
    }

    protected virtual void Awake()
    {
        Instance = this as T;
        DontDestroyOnLoad(this.gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
