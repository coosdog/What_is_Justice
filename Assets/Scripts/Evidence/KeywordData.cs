using UnityEngine;

[CreateAssetMenu(fileName = "KeywordData", menuName = "Game/Investigation/Keyword Data")]
public sealed class KeywordData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string keywordId;
    [SerializeField] private string displayName;

    [Header("Description")]
    [TextArea(2, 5)]
    [SerializeField] private string description;

    public string KeywordId => keywordId;
    public string DisplayName => displayName;
    public string Description => description;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(keywordId))
        {
            keywordId = name;
        }
    }
#endif
}
