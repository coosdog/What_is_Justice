using System;
using UnityEngine;

[Serializable]
public sealed class InventoryItem
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [TextArea(2, 5)]
    [SerializeField] private string inspectResultText;
    [SerializeField] private Sprite icon;
    [SerializeField] private string rewardEvidenceId;
    [SerializeField] private string rewardKeywordId;
    [SerializeField] private bool inspected;

    public InventoryItem(string itemId, string displayName, string description)
        : this(itemId, displayName, description, string.Empty, string.Empty, string.Empty)
    {
    }

    public InventoryItem(
        string itemId,
        string displayName,
        string description,
        string inspectResultText,
        string rewardEvidenceId,
        string rewardKeywordId)
    {
        this.itemId = NormalizeId(itemId);
        this.displayName = string.IsNullOrWhiteSpace(displayName) ? this.itemId : displayName.Trim();
        this.description = description ?? string.Empty;
        this.inspectResultText = inspectResultText ?? string.Empty;
        this.rewardEvidenceId = NormalizeId(rewardEvidenceId);
        this.rewardKeywordId = NormalizeId(rewardKeywordId);
    }

    public InventoryItem(
        string itemId,
        string displayName,
        string description,
        string inspectResultText,
        Sprite icon,
        string rewardEvidenceId,
        string rewardKeywordId)
        : this(itemId, displayName, description, inspectResultText, rewardEvidenceId, rewardKeywordId)
    {
        this.icon = icon;
    }

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public string InspectResultText => inspectResultText;
    public Sprite Icon => icon;
    public string RewardEvidenceId => rewardEvidenceId;
    public string RewardKeywordId => rewardKeywordId;
    public bool Inspected => inspected;
    public bool IsValid => !string.IsNullOrWhiteSpace(itemId);
    public bool HasInspectionReward =>
        !string.IsNullOrWhiteSpace(rewardEvidenceId) || !string.IsNullOrWhiteSpace(rewardKeywordId);

    public bool MatchesId(string id)
    {
        return string.Equals(itemId, NormalizeId(id), StringComparison.OrdinalIgnoreCase);
    }

    public void MarkInspected()
    {
        inspected = true;
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
