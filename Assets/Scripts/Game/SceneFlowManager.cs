using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneFlowManager : PersistentSingleton<SceneFlowManager>
{
    [SerializeField] private string currentSceneName;

    public event Action<string> SceneChanged;

    public string CurrentSceneName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(currentSceneName))
            {
                currentSceneName = SceneManager.GetActiveScene().name;
            }

            return currentSceneName;
        }
        private set
        {
            if (string.Equals(currentSceneName, value, StringComparison.Ordinal))
            {
                return;
            }

            currentSceneName = value;
            SceneChanged?.Invoke(currentSceneName);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (!IsActiveSingleton)
        {
            return;
        }

        CurrentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        SceneManager.LoadScene(sceneName.Trim());
    }

    public void ReloadCurrentScene()
    {
        LoadScene(CurrentSceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentSceneName = scene.name;
    }
}
