using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class CsvDialogueDatabase : MonoBehaviour
{
    [Header("Dialogue Source")]
    [SerializeField] private DialogueCsvCollection csvCollection;

    private readonly Dictionary<string, DialogueEntry> _entriesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SpeakerProfile> _speakerProfilesByKey = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        LoadAll();
    }

    public bool TryGetEntry(string id, out DialogueEntry entry)
    {
        entry = null;
        return !string.IsNullOrWhiteSpace(id) && _entriesById.TryGetValue(id.Trim(), out entry);
    }

    public string GetText(string id, string fallback = "")
    {
        return TryGetEntry(id, out DialogueEntry entry) ? entry.Text : fallback;
    }

    public bool TryGetSpeakerProfile(string speakerKey, out SpeakerProfile profile)
    {
        profile = null;
        return !string.IsNullOrWhiteSpace(speakerKey) && _speakerProfilesByKey.TryGetValue(speakerKey.Trim(), out profile);
    }

    public string ResolveSpeakerDisplayName(string speakerKey)
    {
        return TryGetSpeakerProfile(speakerKey, out SpeakerProfile profile) ? profile.DisplayName : speakerKey;
    }

    public string ResolveDefaultPortraitKey(string speakerKey)
    {
        return TryGetSpeakerProfile(speakerKey, out SpeakerProfile profile) ? profile.DefaultPortraitKey : string.Empty;
    }

    public void LoadAll()
    {
        _entriesById.Clear();
        _speakerProfilesByKey.Clear();
        LoadBuiltInSpeakerProfiles();

        if (csvCollection == null)
        {
            Debug.LogWarning("CsvDialogueDatabase has no DialogueCsvCollection assigned.");
            return;
        }

        bool loadedAny = false;
        HashSet<TextAsset> loadedFiles = new();

        foreach (TextAsset csvFile in csvCollection.EnumerateCsvFiles())
        {
            if (!loadedFiles.Add(csvFile))
            {
                continue;
            }

            LoadCsv(csvFile.text, csvFile.name);
            loadedAny = true;
        }

        if (!loadedAny)
        {
            Debug.LogWarning("DialogueCsvCollection has no CSV files assigned.");
        }
    }

    private void LoadCsv(string csvText, string sourceName)
    {
        List<List<string>> rows = ParseCsv(csvText);
        if (rows.Count <= 1)
        {
            return;
        }

        Dictionary<string, int> headers = BuildHeaderMap(rows[0]);

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            LoadSpeakerProfileRow(row, headers);

            string id = GetCell(row, headers, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            string speakerKey = GetCell(row, headers, "speaker_key");
            if (string.IsNullOrWhiteSpace(speakerKey))
            {
                speakerKey = GetCell(row, headers, "speaker");
            }

            DialogueEntry entry = new DialogueEntry(
                id.Trim(),
                speakerKey,
                GetCell(row, headers, "text"),
                GetCell(row, headers, "portrait_key"),
                GetCell(row, headers, "emotion"),
                GetCell(row, headers, "board_node_id"),
                GetCell(row, headers, "board_display_name"),
                GetCell(row, headers, "board_description"),
                GetOptionalBool(row, headers, "show_portraits", true),
                GetCell(row, headers, "portrait_layout"));

            if (_entriesById.ContainsKey(entry.Id))
            {
                Debug.LogWarning($"Duplicate dialogue id '{entry.Id}' in {sourceName}. The later row replaced the previous one.");
            }

            _entriesById[entry.Id] = entry;
        }
    }

    private void LoadBuiltInSpeakerProfiles()
    {
        AddSpeakerProfile(new SpeakerProfile("child_owl", "손주 올빼미", "child_owl", "player"));
        AddSpeakerProfile(new SpeakerProfile("player.child_owl", "손주 올빼미", "child_owl", "player"));
        AddSpeakerProfile(new SpeakerProfile("player", "손주 올빼미", "child_owl", "player"));
        AddSpeakerProfile(new SpeakerProfile("old_owl", "할아버지 올빼미", "old_owl", "npc"));
        AddSpeakerProfile(new SpeakerProfile("npc.old_owl", "할아버지 올빼미", "old_owl", "npc"));
        AddSpeakerProfile(new SpeakerProfile("grandfather", "할아버지 올빼미", "old_owl", "npc"));
        AddSpeakerProfile(new SpeakerProfile("system_narration", "나레이션", "narration", "system"));
        AddSpeakerProfile(new SpeakerProfile("system.narration", "나레이션", "narration", "system"));
        AddSpeakerProfile(new SpeakerProfile("narration", "나레이션", "narration", "system"));
        AddSpeakerProfile(new SpeakerProfile("system", "시스템", "system", "system"));
    }

    private void LoadSpeakerProfileRow(List<string> row, Dictionary<string, int> headers)
    {
        string speakerKey = GetCell(row, headers, "speaker_key");
        string displayName = GetCell(row, headers, "display_name");
        if (string.IsNullOrWhiteSpace(speakerKey) || string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        AddSpeakerProfile(new SpeakerProfile(
            speakerKey,
            displayName,
            GetCell(row, headers, "default_portrait_key"),
            GetCell(row, headers, "type")));
    }

    private void AddSpeakerProfile(SpeakerProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.SpeakerKey))
        {
            return;
        }

        _speakerProfilesByKey[profile.SpeakerKey] = profile;
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

    private static bool GetOptionalBool(List<string> row, Dictionary<string, int> headers, string columnName, bool fallback)
    {
        string value = GetCell(row, headers, columnName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        value = value.Trim();
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("y", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
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
