using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    protected static T instance;
    public static T Instance
    {
        get { 
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    DontDestroyOnLoad(obj);
                    obj.name = typeof(T).ToString();
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }
}
