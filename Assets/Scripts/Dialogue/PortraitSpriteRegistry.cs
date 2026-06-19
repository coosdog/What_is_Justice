using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PortraitSpriteRegistry : MonoBehaviour
{
    [SerializeField] private PortraitSpriteBinding[] portraits = Array.Empty<PortraitSpriteBinding>();

    public bool TryGetSprite(string portraitKey, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(portraitKey))
        {
            return false;
        }

        foreach (PortraitSpriteBinding portrait in portraits ?? Array.Empty<PortraitSpriteBinding>())
        {
            if (portrait != null && portrait.Matches(portraitKey) && portrait.Sprite != null)
            {
                sprite = portrait.Sprite;
                return true;
            }
        }

#if UNITY_EDITOR
        if (TryGetEditorFallbackSprite(portraitKey, out sprite))
        {
            return true;
        }
#endif

        return false;
    }

#if UNITY_EDITOR
    private static bool TryGetEditorFallbackSprite(string portraitKey, out Sprite sprite)
    {
        sprite = null;
        string path = ResolveEditorFallbackPath(portraitKey);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite loadedSprite)
            {
                sprite = loadedSprite;
                return true;
            }
        }

        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return sprite != null;
    }

    private static string ResolveEditorFallbackPath(string portraitKey)
    {
        if (IsAnyKey(portraitKey, "player", "child", "child_owl", "rookie_owl"))
        {
            return "Assets/Sprites/Child_Owl.png";
        }

        if (IsAnyKey(portraitKey, "old_owl", "grandfather", "mentor"))
        {
            return "Assets/Sprites/Old_Owl.png";
        }

        return string.Empty;
    }

    private static bool IsAnyKey(string key, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (string.Equals(key, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
#endif

    [Serializable]
    public sealed class PortraitSpriteBinding
    {
        [SerializeField] private string portraitKey;
        [SerializeField] private Sprite sprite;

        public Sprite Sprite => sprite;

        public bool Matches(string key)
        {
            return !string.IsNullOrWhiteSpace(portraitKey) &&
                   string.Equals(portraitKey.Trim(), key?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
