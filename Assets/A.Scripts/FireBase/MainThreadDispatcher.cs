using UnityEngine;
using System;
using System.Collections.Generic;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private static Queue<Action> executionQueue = new Queue<Action>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }

    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;

        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("MainThreadDispatcher");
            instance = obj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
    }
}