using UnityEngine;

public sealed class SaveManager : PersistentSingleton<SaveManager>
{
    public bool HasKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && PlayerPrefs.HasKey(key);
    }

    public string GetString(string key, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(key) ? fallback : PlayerPrefs.GetString(key, fallback);
    }

    public void SetString(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        PlayerPrefs.SetString(key, value ?? string.Empty);
    }

    public int GetInt(string key, int fallback = 0)
    {
        return string.IsNullOrWhiteSpace(key) ? fallback : PlayerPrefs.GetInt(key, fallback);
    }

    public void SetInt(string key, int value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            PlayerPrefs.SetInt(key, value);
        }
    }

    public void Save()
    {
        PlayerPrefs.Save();
    }
}
