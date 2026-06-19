using System;
using UnityEngine;

[Serializable]
public sealed class MantraPart
{
    [SerializeField] private string partId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private MantraPartType type;
    [SerializeField] private Sprite icon;

    public MantraPart(string partId, string displayName, string description, MantraPartType type, Sprite icon = null)
    {
        this.partId = partId;
        this.displayName = displayName;
        this.description = description;
        this.type = type;
        this.icon = icon;
    }

    public string PartId => partId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? partId : displayName;
    public string Description => description ?? string.Empty;
    public MantraPartType Type => type;
    public Sprite Icon => icon;
    public bool IsValid => !string.IsNullOrWhiteSpace(partId);

    public bool MatchesId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
            string.Equals(partId, id.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
