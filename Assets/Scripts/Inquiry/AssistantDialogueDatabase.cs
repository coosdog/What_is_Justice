using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class AssistantDialogueDatabase : MonoBehaviour
{
    [SerializeField] private TextAsset assistantDialoguesCsv;

    private readonly List<CsvAssistantDialogueRecord> _records = new();

    private void Awake()
    {
        LoadAll();
    }

    public bool TryGetDialogue(AssistantDialogueTrigger triggerType, string conditionId, PlayerDisposition disposition, out CsvAssistantDialogueRecord record)
    {
        string normalizedCondition = NormalizeCondition(conditionId);

        return TryFind(triggerType, normalizedCondition, disposition.ToString(), out record) ||
               TryFind(triggerType, normalizedCondition, "basic", out record) ||
               TryFind(triggerType, "none", disposition.ToString(), out record) ||
               TryFind(triggerType, "none", "basic", out record);
    }

    public void LoadAll()
    {
        EnsureDefaultCsvSource();
        _records.Clear();

        foreach (List<string> row in ReadDataRows(assistantDialoguesCsv, out Dictionary<string, int> headers))
        {
            string triggerText = GetCell(row, headers, "trigger_type");
            if (!Enum.TryParse(triggerText, true, out AssistantDialogueTrigger triggerType))
            {
                continue;
            }

            CsvAssistantDialogueRecord record = new CsvAssistantDialogueRecord(
                GetCell(row, headers, "assistant_dialogue_id"),
                triggerType,
                GetCell(row, headers, "condition_id"),
                GetCell(row, headers, "disposition"),
                SplitIds(GetCell(row, headers, "response_dialogue_ids")),
                GetCell(row, headers, "fallback_text"));

            if (!string.IsNullOrWhiteSpace(record.AssistantDialogueId))
            {
                _records.Add(record);
            }
        }
    }

    private bool TryFind(AssistantDialogueTrigger triggerType, string conditionId, string disposition, out CsvAssistantDialogueRecord record)
    {
        foreach (CsvAssistantDialogueRecord candidate in _records)
        {
            if (candidate.TriggerType != triggerType)
            {
                continue;
            }

            if (!string.Equals(NormalizeCondition(candidate.ConditionId), conditionId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsDispositionMatch(candidate.Disposition, disposition))
            {
                record = candidate;
                return true;
            }
        }

        record = null;
        return false;
    }

    private void EnsureDefaultCsvSource()
    {
#if UNITY_EDITOR
        assistantDialoguesCsv ??= AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/CSV/assistant_dialogues.csv");
#endif

        assistantDialoguesCsv ??= Resources.Load<TextAsset>("CSV/assistant_dialogues");
    }

    private static bool IsDispositionMatch(string candidate, string expected)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = "basic";
        }

        return string.Equals(candidate.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCondition(string conditionId)
    {
        return string.IsNullOrWhiteSpace(conditionId) ? "none" : conditionId.Trim();
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
