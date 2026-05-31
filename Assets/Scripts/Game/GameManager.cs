using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : PersistentSingleton<GameManager>
{
    [SerializeField] private string currentChapterId;
    [SerializeField] private bool isPaused;

    public event Action<string> ChapterChanged;
    public event Action<bool> PauseChanged;

    public string CurrentChapterId
    {
        get => currentChapterId;
        set
        {
            string nextChapterId = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
            if (string.Equals(currentChapterId, nextChapterId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            currentChapterId = nextChapterId;
            ChapterChanged?.Invoke(currentChapterId);
        }
    }

    public bool IsPaused
    {
        get => isPaused;
        set
        {
            if (isPaused == value)
            {
                return;
            }

            isPaused = value;
            Time.timeScale = isPaused ? 0f : 1f;
            PauseChanged?.Invoke(isPaused);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (!IsActiveSingleton)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(currentChapterId))
        {
            currentChapterId = InferChapterId(SceneManager.GetActiveScene().name);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (IsActiveSingleton)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Time.timeScale = 1f;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentChapterId = InferChapterId(scene.name);
    }

    private static string InferChapterId(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return "Unknown";
        }

        if (sceneName.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Tutorial";
        }

        if (sceneName.IndexOf("Chapter1", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Chapter1";
        }

        if (sceneName.IndexOf("Chapter2", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Chapter2";
        }

        if (sceneName.IndexOf("Chapter3", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Chapter3";
        }

        return sceneName.Trim();
    }
}
