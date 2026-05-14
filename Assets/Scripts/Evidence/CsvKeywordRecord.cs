public sealed class CsvKeywordRecord
{
    public string KeywordId { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public CsvKeywordRecord(string keywordId, string displayName, string description)
    {
        KeywordId = keywordId?.Trim();
        DisplayName = displayName;
        Description = description;
    }
}
