using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvidenceData", menuName = "Game/Investigation/Evidence Data")]
public sealed class EvidenceData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string evidenceId;
    [SerializeField] private string displayName;

    [Header("Description")]
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Unlocked Keywords")]
    [SerializeField] private KeywordData[] unlockedKeywords;

    public string EvidenceId => evidenceId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<KeywordData> UnlockedKeywords => unlockedKeywords ?? Array.Empty<KeywordData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(evidenceId))
        {
            evidenceId = name;
        }
    }
#endif
}
