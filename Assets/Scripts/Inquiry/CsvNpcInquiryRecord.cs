public sealed class CsvNpcInquiryRecord
{
    public string NpcId { get; }
    public string DisplayName { get; }
    public string[] NoKeywordDialogueIds { get; }
    public string[] UnknownKeywordDialogueIds { get; }
    public string NoKeywordFallbackText { get; }
    public string UnknownKeywordFallbackText { get; }

    public CsvNpcInquiryRecord(string npcId, string displayName, string[] noKeywordDialogueIds, string[] unknownKeywordDialogueIds, string noKeywordFallbackText, string unknownKeywordFallbackText)
    {
        NpcId = npcId?.Trim();
        DisplayName = displayName;
        NoKeywordDialogueIds = noKeywordDialogueIds ?? System.Array.Empty<string>();
        UnknownKeywordDialogueIds = unknownKeywordDialogueIds ?? System.Array.Empty<string>();
        NoKeywordFallbackText = noKeywordFallbackText;
        UnknownKeywordFallbackText = unknownKeywordFallbackText;
    }
}
