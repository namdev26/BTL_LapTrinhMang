using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            // Tìm trong scene
            instance = FindAnyObjectByType<UnityMainThreadDispatcher>();
            if (instance == null)
            {
                Debug.LogError("❌ UnityMainThreadDispatcher chưa có trong scene. Hãy tạo 1 GameObject và gắn script này vào.");
            }
        }
        return instance;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene
        }
        else if (instance != this)
        {
            Destroy(gameObject); // tránh tạo 2 dispatcher
        }
    }

    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Đưa 1 action vào queue để chạy trên Main Thread
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}
