public sealed class CsvEvidenceRecord
{
    public string EvidenceId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string[] UnlockedKeywordIds { get; }

    public CsvEvidenceRecord(string evidenceId, string displayName, string description, string[] unlockedKeywordIds)
    {
        EvidenceId = evidenceId?.Trim();
        DisplayName = displayName;
        Description = description;
        UnlockedKeywordIds = unlockedKeywordIds ?? System.Array.Empty<string>();
    }
}
