using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class NpcInteractionRuleDatabase : MonoBehaviour
{
    [SerializeField] private TextAsset interactionRulesCsv;

    private readonly Dictionary<string, List<CsvNpcInteractionRuleRecord>> _rulesByNpcAction = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        Load();
    }

    public IReadOnlyList<CsvNpcInteractionRuleRecord> GetRules(string npcId, string action)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(action))
        {
            return Array.Empty<CsvNpcInteractionRuleRecord>();
        }

        return _rulesByNpcAction.TryGetValue(BuildKey(npcId, action), out List<CsvNpcInteractionRuleRecord> rules)
            ? rules
            : Array.Empty<CsvNpcInteractionRuleRecord>();
    }

    public void Load()
    {
        _rulesByNpcAction.Clear();

        TextAsset csv = interactionRulesCsv;
        if (csv == null)
        {
            csv = Resources.Load<TextAsset>("CSV/npc_interaction_rules");
        }

        if (csv == null)
        {
            Debug.LogWarning("NpcInteractionRuleDatabase has no npc_interaction_rules CSV assigned.");
            return;
        }

        LoadCsv(csv.text);
    }

    private void LoadCsv(string csvText)
    {
        List<List<string>> rows = ParseCsv(csvText);
        if (rows.Count <= 1)
        {
            return;
        }

        Dictionary<string, int> headers = BuildHeaderMap(rows[0]);
        for (int i = 1; i < rows.Count; i++)
        {
            CsvNpcInteractionRuleRecord record = BuildRecord(rows[i], headers);
            if (record == null || !record.IsValid)
            {
                continue;
            }

            string key = BuildKey(record.NpcId, record.Action);
            if (!_rulesByNpcAction.TryGetValue(key, out List<CsvNpcInteractionRuleRecord> rules))
            {
                rules = new List<CsvNpcInteractionRuleRecord>();
                _rulesByNpcAction.Add(key, rules);
            }

            rules.Add(record);
        }

        foreach (List<CsvNpcInteractionRuleRecord> rules in _rulesByNpcAction.Values)
        {
            rules.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        }
    }

    private static CsvNpcInteractionRuleRecord BuildRecord(List<string> row, Dictionary<string, int> headers)
    {
        return new CsvNpcInteractionRuleRecord(
            GetCell(row, headers, "rule_id"),
            GetCell(row, headers, "npc_id"),
            GetCell(row, headers, "action"),
            ParseInt(GetCell(row, headers, "priority")),
            GetCell(row, headers, "condition_type"),
            GetCell(row, headers, "condition_value"),
            GetCell(row, headers, "result_type"),
            GetCell(row, headers, "result_value"),
            ParseBool(GetCell(row, headers, "run_once")));
    }

    private static string BuildKey(string npcId, string action)
    {
        return $"{npcId.Trim()}|{action.Trim()}";
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out int result) ? result : 0;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("y", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
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
