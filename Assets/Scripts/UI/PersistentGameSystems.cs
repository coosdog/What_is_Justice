using UnityEngine;

public sealed class PersistentGameSystems : MonoBehaviour
{
    private static PersistentGameSystems _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
