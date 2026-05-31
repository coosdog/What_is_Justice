using UnityEngine;

public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting;

    public static bool HasInstance => _instance != null;
    protected bool IsActiveSingleton => ReferenceEquals(_instance, this);

    public static T Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            if (_isQuitting)
            {
                return null;
            }

            _instance = FindFirstObjectByType<T>();
            if (_instance != null)
            {
                return _instance;
            }

            GameObject singletonObject = new(typeof(T).Name);
            _instance = singletonObject.AddComponent<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}
