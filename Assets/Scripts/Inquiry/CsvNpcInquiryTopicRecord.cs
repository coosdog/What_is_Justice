public sealed class CsvNpcInquiryTopicRecord
{
    public string NpcId { get; }
    public string KeywordId { get; }
    public string[] ResponseDialogueIds { get; }
    public string FallbackResponseText { get; }

    public CsvNpcInquiryTopicRecord(string npcId, string keywordId, string[] responseDialogueIds, string fallbackResponseText)
    {
        NpcId = npcId?.Trim();
        KeywordId = keywordId?.Trim();
        ResponseDialogueIds = responseDialogueIds ?? System.Array.Empty<string>();
        FallbackResponseText = fallbackResponseText;
    }
}
