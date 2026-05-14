using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CsvInvestigationDatabase : MonoBehaviour
{
    [Header("CSV Sources")]
    [SerializeField] private TextAsset keywordsCsv;
    [SerializeField] private TextAsset evidenceCsv;
    [SerializeField] private TextAsset npcInquiriesCsv;
    [SerializeField] private TextAsset npcInquiryTopicsCsv;

    private readonly Dictionary<string, CsvKeywordRecord> _keywordsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CsvEvidenceRecord> _evidenceById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CsvNpcInquiryRecord> _npcInquiriesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<CsvNpcInquiryTopicRecord>> _topicsByNpcId = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        LoadAll();
    }

    public bool TryGetKeyword(string keywordId, out CsvKeywordRecord keyword)
    {
        keyword = null;
        return !string.IsNullOrWhiteSpace(keywordId) && _keywordsById.TryGetValue(keywordId.Trim(), out keyword);
    }

    public bool TryGetEvidence(string evidenceId, out CsvEvidenceRecord evidence)
    {
        evidence = null;
        return !string.IsNullOrWhiteSpace(evidenceId) && _evidenceById.TryGetValue(evidenceId.Trim(), out evidence);
    }

    public bool TryGetNpcInquiry(string npcId, out CsvNpcInquiryRecord inquiry)
    {
        inquiry = null;
        return !string.IsNullOrWhiteSpace(npcId) && _npcInquiriesById.TryGetValue(npcId.Trim(), out inquiry);
    }

    public bool TryGetNpcTopic(string npcId, string keywordId, out CsvNpcInquiryTopicRecord topic)
    {
        topic = null;
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(keywordId))
        {
            return false;
        }

        if (!_topicsByNpcId.TryGetValue(npcId.Trim(), out List<CsvNpcInquiryTopicRecord> topics))
        {
            return false;
        }

        foreach (CsvNpcInquiryTopicRecord candidate in topics)
        {
            if (string.Equals(candidate.KeywordId, keywordId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                topic = candidate;
                return true;
            }
        }

        return false;
    }

    public void LoadAll()
    {
        EnsureDefaultCsvSources();

        _keywordsById.Clear();
        _evidenceById.Clear();
        _npcInquiriesById.Clear();
        _topicsByNpcId.Clear();

        LoadKeywords();
        LoadEvidence();
        LoadNpcInquiries();
        LoadNpcInquiryTopics();
    }

    private void EnsureDefaultCsvSources()
    {
#if UNITY_EDITOR
        keywordsCsv ??= AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/keywords.csv");
        evidenceCsv ??= AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/evidence.csv");
        npcInquiriesCsv ??= AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/npc_inquiries.csv");
        npcInquiryTopicsCsv ??= AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/npc_inquiry_topics.csv");
#endif

        keywordsCsv ??= Resources.Load<TextAsset>("CSV/keywords");
        evidenceCsv ??= Resources.Load<TextAsset>("CSV/evidence");
        npcInquiriesCsv ??= Resources.Load<TextAsset>("CSV/npc_inquiries");
        npcInquiryTopicsCsv ??= Resources.Load<TextAsset>("CSV/npc_inquiry_topics");
    }

    private void LoadKeywords()
    {
        foreach (List<string> row in ReadDataRows(keywordsCsv, out Dictionary<string, int> headers))
        {
            CsvKeywordRecord record = new CsvKeywordRecord(
                GetCell(row, headers, "keyword_id"),
                GetCell(row, headers, "display_name"),
                GetCell(row, headers, "description"));

            if (!string.IsNullOrWhiteSpace(record.KeywordId))
            {
                _keywordsById[record.KeywordId] = record;
            }
        }
    }

    private void LoadEvidence()
    {
        foreach (List<string> row in ReadDataRows(evidenceCsv, out Dictionary<string, int> headers))
        {
            CsvEvidenceRecord record = new CsvEvidenceRecord(
                GetCell(row, headers, "evidence_id"),
                GetCell(row, headers, "display_name"),
                GetCell(row, headers, "description"),
                SplitIds(GetCell(row, headers, "unlocked_keyword_ids")));

            if (!string.IsNullOrWhiteSpace(record.EvidenceId))
            {
                _evidenceById[record.EvidenceId] = record;
            }
        }
    }

    private void LoadNpcInquiries()
    {
        foreach (List<string> row in ReadDataRows(npcInquiriesCsv, out Dictionary<string, int> headers))
        {
            CsvNpcInquiryRecord record = new CsvNpcInquiryRecord(
                GetCell(row, headers, "npc_id"),
                GetCell(row, headers, "display_name"),
                SplitIds(GetCell(row, headers, "no_keyword_dialogue_ids")),
                SplitIds(GetCell(row, headers, "unknown_keyword_dialogue_ids")),
                GetCell(row, headers, "no_keyword_fallback_text"),
                GetCell(row, headers, "unknown_keyword_fallback_text"));

            if (!string.IsNullOrWhiteSpace(record.NpcId))
            {
                _npcInquiriesById[record.NpcId] = record;
            }
        }
    }

    private void LoadNpcInquiryTopics()
    {
        foreach (List<string> row in ReadDataRows(npcInquiryTopicsCsv, out Dictionary<string, int> headers))
        {
            CsvNpcInquiryTopicRecord record = new CsvNpcInquiryTopicRecord(
                GetCell(row, headers, "npc_id"),
                GetCell(row, headers, "keyword_id"),
                SplitIds(GetCell(row, headers, "response_dialogue_ids")),
                GetCell(row, headers, "fallback_response_text"));

            if (string.IsNullOrWhiteSpace(record.NpcId) || string.IsNullOrWhiteSpace(record.KeywordId))
            {
                continue;
            }

            if (!_topicsByNpcId.TryGetValue(record.NpcId, out List<CsvNpcInquiryTopicRecord> topics))
            {
                topics = new List<CsvNpcInquiryTopicRecord>();
                _topicsByNpcId.Add(record.NpcId, topics);
            }

            topics.Add(record);
        }
    }

    private static List<List<string>> ReadDataRows(TextAsset csvFile, out Dictionary<string, int> headers)
    {
        headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        List<List<string>> dataRows = new();

        if (csvFile == null)
        {
            return dataRows;
        }

        List<List<string>> rows = ParseCsv(csvFile.text);
        if (rows.Count <= 1)
        {
            return dataRows;
        }

        headers = BuildHeaderMap(rows[0]);
        for (int i = 1; i < rows.Count; i++)
        {
            dataRows.Add(rows[i]);
        }

        return dataRows;
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headerRow)
    {
        Dictionary<string, int> headers = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            string header = headerRow[i]?.Trim();
            if (!string.IsNullOrEmpty(header) && !headers.ContainsKey(header))
            {
                headers.Add(header, i);
            }
        }

        return headers;
    }

    private static string GetCell(List<string> row, Dictionary<string, int> headers, string columnName)
    {
        return headers.TryGetValue(columnName, out int index) && index >= 0 && index < row.Count
            ? row[index]
            : string.Empty;
    }

    private static string[] SplitIds(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        string[] rawIds = value.Split('|');
        List<string> ids = new();
        foreach (string rawId in rawIds)
        {
            string id = rawId.Trim();
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }

        return ids.ToArray();
    }

    private static List<List<string>> ParseCsv(string text)
    {
        List<List<string>> rows = new();
        List<string> row = new();
        StringBuilder cell = new();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];

            if (current == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (current == ',' && !inQuotes)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if ((current == '\n' || current == '\r') && !inQuotes)
            {
                if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Clear();
                if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
                {
                    rows.Add(row);
                }

                row = new List<string>();
            }
            else
            {
                cell.Append(current);
            }
        }

        row.Add(cell.ToString());
        if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
        {
            rows.Add(row);
        }

        return rows;
    }
}

