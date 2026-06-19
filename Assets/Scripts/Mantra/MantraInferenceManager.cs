using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MantraInferenceManager : MonoBehaviour
{
    private const string MainMantraAssetPath = "Assets/Sprites/MainMantra";
    private const string SubMantraAssetPath = "Assets/Sprites/SubMantra";
    private const string GuideMantraAssetPath = "Assets/Sprites/GuideMantra";
    private const string MantraPartsCsvPath = "Assets/Data/CSV/mantra_parts.csv";
    private const string MantraResultsCsvPath = "Assets/Data/CSV/mantra_results.csv";

    [Header("CSV Sources")]
    [SerializeField] private TextAsset mantraPartsCsv;
    [SerializeField] private TextAsset mantraResultsCsv;

    [SerializeField] private MantraPart[] availableParts;
    [SerializeField] private MantraRecipe[] recipes;
    [SerializeField] private string selectedMainImageId;
    [SerializeField] private string selectedSupportImageId;
    [SerializeField] private string selectedMinorPatternId;

    private static readonly string[] EmptyMinorPatternSelection = Array.Empty<string>();
    private bool _loadedMantraAssets;

    public event Action Changed;

    public string SelectedMainImageId
    {
        get => selectedMainImageId;
        set
        {
            selectedMainImageId = NormalizeId(value);
            Changed?.Invoke();
        }
    }

    public string SelectedSupportImageId
    {
        get => selectedSupportImageId;
        set
        {
            selectedSupportImageId = NormalizeId(value);
            Changed?.Invoke();
        }
    }

    public string SelectedMinorPatternId => selectedMinorPatternId;
    public IReadOnlyList<string> SelectedMinorPatternIds =>
        string.IsNullOrWhiteSpace(selectedMinorPatternId) ? EmptyMinorPatternSelection : new[] { selectedMinorPatternId };
    public IReadOnlyList<MantraPart> AvailableParts => availableParts;
    public IReadOnlyList<MantraRecipe> Recipes => recipes;
    public string SelectedCombinationKey => BuildCombinationKey(selectedMainImageId, selectedSupportImageId, selectedMinorPatternId);

    private void Awake()
    {
        EnsureDefaults();
        selectedMainImageId = NormalizeId(selectedMainImageId);
        selectedSupportImageId = NormalizeId(selectedSupportImageId);
        selectedMinorPatternId = NormalizeId(selectedMinorPatternId);
    }

    public IReadOnlyList<MantraPart> GetParts(MantraPartType type)
    {
        EnsureDefaults();
        return availableParts.Where(part => part != null && part.IsValid && part.Type == type).ToArray();
    }

    public bool TryGetSelectedPart(MantraPartType type, out MantraPart selectedPart)
    {
        EnsureDefaults();
        string selectedId = type switch
        {
            MantraPartType.MainImage => selectedMainImageId,
            MantraPartType.SupportImage => selectedSupportImageId,
            MantraPartType.MinorPattern => selectedMinorPatternId,
            _ => string.Empty
        };

        selectedPart = availableParts.FirstOrDefault(part =>
            part != null &&
            part.Type == type &&
            string.Equals(part.PartId, selectedId, StringComparison.OrdinalIgnoreCase));
        return selectedPart != null;
    }

    public void SelectMainImage(string partId)
    {
        SelectedMainImageId = partId;
    }

    public void SelectSupportImage(string partId)
    {
        SelectedSupportImageId = partId;
    }

    public void ToggleMinorPattern(string partId)
    {
        SelectMinorPattern(string.Equals(selectedMinorPatternId, NormalizeId(partId), StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : partId);
    }

    public void SelectMinorPattern(string partId)
    {
        selectedMinorPatternId = NormalizeId(partId);
        Changed?.Invoke();
    }

    public void ClearSelection()
    {
        selectedMainImageId = string.Empty;
        selectedSupportImageId = string.Empty;
        selectedMinorPatternId = string.Empty;
        Changed?.Invoke();
    }

    public MantraInferenceResult Infer()
    {
        EnsureDefaults();
        selectedMainImageId = NormalizeId(selectedMainImageId);
        selectedSupportImageId = NormalizeId(selectedSupportImageId);
        selectedMinorPatternId = NormalizeId(selectedMinorPatternId);

        if (string.IsNullOrWhiteSpace(selectedMainImageId))
        {
            return MantraInferenceResult.Failed("\uAC00\uC6B4\uB370 \uC8FC \uB9CC\uD2B8\uB77C\uAC00 \uC120\uD0DD\uB418\uC9C0 \uC54A\uC558\uB2E4.");
        }

        if (string.IsNullOrWhiteSpace(selectedSupportImageId))
        {
            return MantraInferenceResult.Failed("\uC8FC \uB9CC\uD2B8\uB77C\uB97C \uBCF4\uC870\uD560 \uBCF4\uC870 \uB9CC\uD2B8\uB77C\uAC00 \uC120\uD0DD\uB418\uC9C0 \uC54A\uC558\uB2E4.");
        }

        if (string.IsNullOrWhiteSpace(selectedMinorPatternId))
        {
            return MantraInferenceResult.Failed("\uC678\uACFD\uC744 \uC7A5\uC2DD\uD560 \uC7A5\uC2DD \uB9CC\uD2B8\uB77C\uAC00 \uC120\uD0DD\uB418\uC9C0 \uC54A\uC558\uB2E4.");
        }

        foreach (MantraRecipe recipe in recipes)
        {
            if (recipe != null && recipe.IsValid && IsRecipeMatched(recipe))
            {
                return MantraInferenceResult.Match(recipe);
            }
        }

        return MantraInferenceResult.Failed("\uC120\uD0DD\uD55C \uC870\uD569\uACFC \uC77C\uCE58\uD558\uB294 \uB9CC\uD2B8\uB77C \uAE30\uB85D\uC744 \uCC3E\uC9C0 \uBABB\uD588\uB2E4.");
    }

    private bool IsRecipeMatched(MantraRecipe recipe)
    {
        if (!string.Equals(recipe.MainImageId, selectedMainImageId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(recipe.SupportImageId, selectedSupportImageId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string requiredGuideId = recipe.MinorPatternIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(NormalizeId)
            .FirstOrDefault();

        return string.Equals(requiredGuideId, selectedMinorPatternId, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureDefaults()
    {
        if (!_loadedMantraAssets)
        {
            _loadedMantraAssets = true;
            Dictionary<string, MantraPartMetadata> metadataByKey = LoadPartMetadata();
            List<MantraPart> parts = new();
            parts.AddRange(LoadSpriteParts(MainMantraAssetPath, "MainMantra", MantraPartType.MainImage, metadataByKey));
            parts.AddRange(LoadSpriteParts(SubMantraAssetPath, "SubMantra", MantraPartType.SupportImage, metadataByKey));
            parts.AddRange(LoadSpriteParts(GuideMantraAssetPath, "GuideMantra", MantraPartType.MinorPattern, metadataByKey));

            if (parts.Count > 0)
            {
                availableParts = parts.ToArray();
                recipes = LoadResultRecipes();
                return;
            }
        }

        if (availableParts != null && availableParts.Length > 0)
        {
            return;
        }

        availableParts = new[]
        {
            new MantraPart("main.placeholder", "\uC8FC \uB9CC\uD2B8\uB77C \uC5C6\uC74C", "Assets/Sprites/MainMantra \uD3F4\uB354\uC5D0 \uC2A4\uD504\uB77C\uC774\uD2B8\uB97C \uB123\uC5B4\uC8FC\uC138\uC694.", MantraPartType.MainImage),
            new MantraPart("sub.placeholder", "\uBCF4\uC870 \uB9CC\uD2B8\uB77C \uC5C6\uC74C", "Assets/Sprites/SubMantra \uD3F4\uB354\uC5D0 \uC2A4\uD504\uB77C\uC774\uD2B8\uB97C \uB123\uC5B4\uC8FC\uC138\uC694.", MantraPartType.SupportImage),
            new MantraPart("guide.placeholder", "\uC7A5\uC2DD \uB9CC\uD2B8\uB77C \uC5C6\uC74C", "Assets/Sprites/GuideMantra \uD3F4\uB354\uC5D0 \uC2A4\uD504\uB77C\uC774\uD2B8\uB97C \uB123\uC5B4\uC8FC\uC138\uC694.", MantraPartType.MinorPattern)
        };
        recipes = Array.Empty<MantraRecipe>();
    }

    private static IEnumerable<MantraPart> LoadSpriteParts(
        string editorAssetPath,
        string resourcesPath,
        MantraPartType type,
        Dictionary<string, MantraPartMetadata> metadataByKey)
    {
        List<Sprite> sprites = new();
#if UNITY_EDITOR
        if (AssetDatabase.IsValidFolder(editorAssetPath))
        {
            HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { editorAssetPath }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { editorAssetPath }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (string path in paths)
            {
                Sprite sprite = LoadSpriteOrCreateFromTexture(path);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }
        }
#endif
        if (sprites.Count == 0)
        {
            sprites.AddRange(Resources.LoadAll<Sprite>(resourcesPath));
        }

        foreach (Sprite sprite in sprites.OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase))
        {
            string key = sprite.name.Trim();
            metadataByKey.TryGetValue(key, out MantraPartMetadata metadata);
            yield return new MantraPart(
                key,
                string.IsNullOrWhiteSpace(metadata.DisplayName) ? key : metadata.DisplayName,
                metadata.Description,
                type,
                sprite);
        }
    }

#if UNITY_EDITOR
    private static Sprite LoadSpriteOrCreateFromTexture(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        Sprite runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        runtimeSprite.name = texture.name;
        return runtimeSprite;
    }
#endif

    private Dictionary<string, MantraPartMetadata> LoadPartMetadata()
    {
        Dictionary<string, MantraPartMetadata> metadataByKey = new(StringComparer.OrdinalIgnoreCase);
        foreach (List<string> row in ReadDataRows(GetCsvText(mantraPartsCsv, MantraPartsCsvPath, "CSV/mantra_parts"), out Dictionary<string, int> headers))
        {
            string key = GetCell(row, headers, "part_key");
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            metadataByKey[key.Trim()] = new MantraPartMetadata(
                GetCell(row, headers, "display_name"),
                GetCell(row, headers, "description"));
        }

        return metadataByKey;
    }

    private MantraRecipe[] LoadResultRecipes()
    {
        List<MantraRecipe> loadedRecipes = new();
        foreach (List<string> row in ReadDataRows(GetCsvText(mantraResultsCsv, MantraResultsCsvPath, "CSV/mantra_results"), out Dictionary<string, int> headers))
        {
            string combinationKey = GetCell(row, headers, "combination_key");
            if (string.IsNullOrWhiteSpace(combinationKey))
            {
                continue;
            }

            string[] keys = combinationKey.Split('|');
            if (keys.Length != 3)
            {
                Debug.LogWarning($"Invalid mantra combination key '{combinationKey}'. Use main_key|sub_key|guide_key.");
                continue;
            }

            loadedRecipes.Add(new MantraRecipe(
                GetCell(row, headers, "result_id"),
                GetCell(row, headers, "display_name"),
                GetCell(row, headers, "description"),
                NormalizeId(keys[0]),
                NormalizeId(keys[1]),
                new[] { NormalizeId(keys[2]) }));
        }

        return loadedRecipes.ToArray();
    }

    private static string GetCsvText(TextAsset explicitCsv, string editorPath, string resourcesPath)
    {
        if (explicitCsv != null)
        {
            return explicitCsv.text;
        }

#if UNITY_EDITOR
        TextAsset editorCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(editorPath);
        if (editorCsv != null)
        {
            return editorCsv.text;
        }
#endif
        TextAsset resourcesCsv = Resources.Load<TextAsset>(resourcesPath);
        return resourcesCsv != null ? resourcesCsv.text : string.Empty;
    }

    private static List<List<string>> ReadDataRows(string csvText, out Dictionary<string, int> headers)
    {
        List<List<string>> rows = ParseCsv(csvText);
        headers = rows.Count > 0 ? BuildHeaderMap(rows[0]) : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        List<List<string>> dataRows = new();
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
            if (!string.IsNullOrWhiteSpace(header) && !headers.ContainsKey(header))
            {
                headers.Add(header, i);
            }
        }

        return headers;
    }

    private static string GetCell(List<string> row, Dictionary<string, int> headers, string columnName)
    {
        return headers.TryGetValue(columnName, out int index) && index >= 0 && index < row.Count
            ? row[index].Trim()
            : string.Empty;
    }

    private static List<List<string>> ParseCsv(string text)
    {
        List<List<string>> rows = new();
        if (string.IsNullOrWhiteSpace(text))
        {
            return rows;
        }

        List<string> row = new();
        StringBuilder cell = new();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
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
            else if (c == ',' && !inQuotes)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
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
                cell.Append(c);
            }
        }

        row.Add(cell.ToString());
        if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static string BuildCombinationKey(string mainId, string supportId, string minorId)
    {
        return $"{NormalizeId(mainId)}|{NormalizeId(supportId)}|{NormalizeId(minorId)}";
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private readonly struct MantraPartMetadata
    {
        public MantraPartMetadata(string displayName, string description)
        {
            DisplayName = displayName;
            Description = description ?? string.Empty;
        }

        public string DisplayName { get; }
        public string Description { get; }
    }
}
