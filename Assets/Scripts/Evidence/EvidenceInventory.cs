using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EvidenceInventory : MonoBehaviour
{
    [SerializeField] private EvidenceData[] startingEvidence;
    [SerializeField] private KeywordData[] startingKeywords;
    [SerializeField] private string[] startingCsvEvidenceIds;
    [SerializeField] private string[] startingCsvKeywordIds;
    [SerializeField] private CsvInvestigationDatabase csvDatabase;

    public event Action<EvidenceData> EvidenceAdded;
    public event Action<KeywordData> KeywordAdded;
    public event Action<CsvEvidenceRecord> CsvEvidenceAdded;
    public event Action<CsvKeywordRecord> CsvKeywordAdded;

    private readonly Dictionary<string, EvidenceData> _evidenceById = new();
    private readonly Dictionary<string, KeywordData> _keywordsById = new();
    private readonly Dictionary<string, CsvEvidenceRecord> _csvEvidenceById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CsvKeywordRecord> _csvKeywordsById = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<EvidenceData> Evidence => _evidenceById.Values;
    public IReadOnlyCollection<KeywordData> Keywords => _keywordsById.Values;
    public IReadOnlyCollection<CsvEvidenceRecord> CsvEvidence => _csvEvidenceById.Values;
    public IReadOnlyCollection<CsvKeywordRecord> CsvKeywords => _csvKeywordsById.Values;
    public bool HasAnyKeyword => _keywordsById.Count > 0 || _csvKeywordsById.Count > 0;

    private void Awake()
    {
        if (csvDatabase == null)
        {
            csvDatabase = FindFirstObjectByType<CsvInvestigationDatabase>();
        }

        if (startingCsvKeywordIds != null)
        {
            foreach (string keywordId in startingCsvKeywordIds)
            {
                AddKeyword(keywordId, false);
            }
        }

        if (startingCsvEvidenceIds != null)
        {
            foreach (string evidenceId in startingCsvEvidenceIds)
            {
                AddEvidence(evidenceId, false);
            }
        }

        if (startingKeywords != null)
        {
            foreach (KeywordData keyword in startingKeywords)
            {
                AddKeyword(keyword, false);
            }
        }

        if (startingEvidence != null)
        {
            foreach (EvidenceData evidence in startingEvidence)
            {
                AddEvidence(evidence, false);
            }
        }
    }

    public bool HasEvidence(EvidenceData evidence) => evidence != null && HasEvidence(evidence.EvidenceId);

    public bool HasEvidence(string evidenceId)
    {
        return !string.IsNullOrWhiteSpace(evidenceId) &&
               (_evidenceById.ContainsKey(evidenceId) || _csvEvidenceById.ContainsKey(evidenceId));
    }

    public bool HasKeyword(KeywordData keyword) => keyword != null && HasKeyword(keyword.KeywordId);

    public bool HasKeyword(string keywordId)
    {
        return !string.IsNullOrWhiteSpace(keywordId) &&
               (_keywordsById.ContainsKey(keywordId) || _csvKeywordsById.ContainsKey(keywordId));
    }

    public bool AddEvidence(EvidenceData evidence) => AddEvidence(evidence, true);
    public bool AddKeyword(KeywordData keyword) => AddKeyword(keyword, true);
    public bool AddEvidence(string evidenceId) => AddEvidence(evidenceId, true);
    public bool AddKeyword(string keywordId) => AddKeyword(keywordId, true);

    private bool AddEvidence(EvidenceData evidence, bool notify)
    {
        if (evidence == null || string.IsNullOrWhiteSpace(evidence.EvidenceId) || HasEvidence(evidence.EvidenceId))
        {
            return false;
        }

        _evidenceById.Add(evidence.EvidenceId, evidence);

        foreach (KeywordData keyword in evidence.UnlockedKeywords)
        {
            AddKeyword(keyword, notify);
        }

        if (notify)
        {
            EvidenceAdded?.Invoke(evidence);
            Debug.Log($"Evidence acquired: {evidence.DisplayName} ({evidence.EvidenceId})");
        }

        return true;
    }

    private bool AddEvidence(string evidenceId, bool notify)
    {
        if (string.IsNullOrWhiteSpace(evidenceId) || HasEvidence(evidenceId))
        {
            return false;
        }

        if (csvDatabase == null || !csvDatabase.TryGetEvidence(evidenceId, out CsvEvidenceRecord evidence))
        {
            Debug.LogWarning($"CSV evidence not found: {evidenceId}");
            return false;
        }

        _csvEvidenceById.Add(evidence.EvidenceId, evidence);

        foreach (string keywordId in evidence.UnlockedKeywordIds)
        {
            AddKeyword(keywordId, notify);
        }

        if (notify)
        {
            CsvEvidenceAdded?.Invoke(evidence);
            Debug.Log($"CSV evidence acquired: {evidence.DisplayName} ({evidence.EvidenceId})");
        }

        return true;
    }

    private bool AddKeyword(KeywordData keyword, bool notify)
    {
        if (keyword == null || string.IsNullOrWhiteSpace(keyword.KeywordId) || HasKeyword(keyword.KeywordId))
        {
            return false;
        }

        _keywordsById.Add(keyword.KeywordId, keyword);

        if (notify)
        {
            KeywordAdded?.Invoke(keyword);
            Debug.Log($"Keyword acquired: {keyword.DisplayName} ({keyword.KeywordId})");
        }

        return true;
    }

    private bool AddKeyword(string keywordId, bool notify)
    {
        if (string.IsNullOrWhiteSpace(keywordId) || HasKeyword(keywordId))
        {
            return false;
        }

        if (csvDatabase == null || !csvDatabase.TryGetKeyword(keywordId, out CsvKeywordRecord keyword))
        {
            Debug.LogWarning($"CSV keyword not found: {keywordId}");
            return false;
        }

        _csvKeywordsById.Add(keyword.KeywordId, keyword);

        if (notify)
        {
            CsvKeywordAdded?.Invoke(keyword);
            Debug.Log($"CSV keyword acquired: {keyword.DisplayName} ({keyword.KeywordId})");
        }

        return true;
    }
}
