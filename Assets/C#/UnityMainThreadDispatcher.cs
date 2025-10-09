using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _actions = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null) return;
        var go = new GameObject("UnityMainThreadDispatcher");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UnityMainThreadDispatcher>();
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }

    void Update()
    {
        // Run queued actions on main thread
        while (true)
        {
            Action action = null;
            lock (_actions)
            {
                if (_actions.Count > 0) action = _actions.Dequeue();
            }

            if (action == null) break;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in UnityMainThreadDispatcher action: {ex}");
            }
        }
    }
}
