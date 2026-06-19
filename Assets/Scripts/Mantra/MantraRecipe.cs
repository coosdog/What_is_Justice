using System;
using UnityEngine;

[Serializable]
public sealed class MantraRecipe
{
    [SerializeField] private string resultId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private string mainImageId;
    [SerializeField] private string supportImageId;
    [SerializeField] private string[] minorPatternIds;

    public MantraRecipe(
        string resultId,
        string displayName,
        string description,
        string mainImageId,
        string supportImageId,
        string[] minorPatternIds)
    {
        this.resultId = resultId;
        this.displayName = displayName;
        this.description = description;
        this.mainImageId = mainImageId;
        this.supportImageId = supportImageId;
        this.minorPatternIds = minorPatternIds;
    }

    public string ResultId => resultId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? resultId : displayName;
    public string Description => description ?? string.Empty;
    public string MainImageId => mainImageId;
    public string SupportImageId => supportImageId;
    public string[] MinorPatternIds => minorPatternIds ?? Array.Empty<string>();
    public bool IsValid => !string.IsNullOrWhiteSpace(resultId) &&
                           !string.IsNullOrWhiteSpace(mainImageId) &&
                           !string.IsNullOrWhiteSpace(supportImageId);
}
