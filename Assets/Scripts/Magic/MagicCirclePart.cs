using System;
using UnityEngine;

[Serializable]
public sealed class MagicCirclePart
{
    [SerializeField] private string partId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private MagicCirclePartType type;

    public MagicCirclePart(string partId, string displayName, string description, MagicCirclePartType type)
    {
        this.partId = partId;
        this.displayName = displayName;
        this.description = description;
        this.type = type;
    }

    public string PartId => partId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? partId : displayName;
    public string Description => description ?? string.Empty;
    public MagicCirclePartType Type => type;
    public bool IsValid => !string.IsNullOrWhiteSpace(partId);

    public bool MatchesId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
            string.Equals(partId, id.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
