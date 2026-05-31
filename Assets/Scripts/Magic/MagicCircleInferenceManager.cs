using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class MagicCircleInferenceManager : MonoBehaviour
{
    [SerializeField] private MagicCirclePart[] availableParts;
    [SerializeField] private MagicCircleRecipe[] recipes;
    [SerializeField] private string selectedMainImageId;
    [SerializeField] private string selectedSupportImageId;
    [SerializeField] private string[] selectedMinorPatternIds;

    private readonly List<string> _selectedMinorPatterns = new();

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

    public IReadOnlyList<string> SelectedMinorPatternIds => _selectedMinorPatterns;
    public IReadOnlyList<MagicCirclePart> AvailableParts => availableParts;
    public IReadOnlyList<MagicCircleRecipe> Recipes => recipes;

    private void Awake()
    {
        EnsureDefaults();
        SyncSerializedMinorPatterns();
    }

    public IReadOnlyList<MagicCirclePart> GetParts(MagicCirclePartType type)
    {
        EnsureDefaults();
        return availableParts.Where(part => part != null && part.IsValid && part.Type == type).ToArray();
    }

    public bool TryGetSelectedPart(MagicCirclePartType type, out MagicCirclePart selectedPart)
    {
        string selectedId = type switch
        {
            MagicCirclePartType.MainImage => selectedMainImageId,
            MagicCirclePartType.SupportImage => selectedSupportImageId,
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
        partId = NormalizeId(partId);
        if (string.IsNullOrWhiteSpace(partId))
        {
            return;
        }

        int index = _selectedMinorPatterns.FindIndex(id => string.Equals(id, partId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _selectedMinorPatterns.RemoveAt(index);
        }
        else
        {
            _selectedMinorPatterns.Add(partId);
        }

        selectedMinorPatternIds = _selectedMinorPatterns.ToArray();
        Changed?.Invoke();
    }

    public void ClearSelection()
    {
        selectedMainImageId = string.Empty;
        selectedSupportImageId = string.Empty;
        _selectedMinorPatterns.Clear();
        selectedMinorPatternIds = Array.Empty<string>();
        Changed?.Invoke();
    }

    public MagicCircleInferenceResult Infer()
    {
        EnsureDefaults();
        SyncSerializedMinorPatterns();

        if (string.IsNullOrWhiteSpace(selectedMainImageId))
        {
            return MagicCircleInferenceResult.Failed("\uAC00\uC6B4\uB370 \uBA54\uC778\uADF8\uB9BC\uC774 \uC120\uD0DD\uB418\uC9C0 \uC54A\uC558\uB2E4.");
        }

        if (string.IsNullOrWhiteSpace(selectedSupportImageId))
        {
            return MagicCircleInferenceResult.Failed("\uBA54\uC778\uADF8\uB9BC\uC744 \uC7A5\uC2DD\uD558\uB294 \uBCF4\uC870\uADF8\uB9BC\uC774 \uC120\uD0DD\uB418\uC9C0 \uC54A\uC558\uB2E4.");
        }

        foreach (MagicCircleRecipe recipe in recipes)
        {
            if (recipe == null || !recipe.IsValid)
            {
                continue;
            }

            if (IsRecipeMatched(recipe))
            {
                return MagicCircleInferenceResult.Match(recipe);
            }
        }

        return MagicCircleInferenceResult.Failed("\uC120\uD0DD\uD55C \uAD6C\uC870\uC640 \uC77C\uCE58\uD558\uB294 \uB9C8\uBC95\uC9C4 \uAE30\uB85D\uC744 \uCC3E\uC9C0 \uBABB\uD588\uB2E4.");
    }

    private bool IsRecipeMatched(MagicCircleRecipe recipe)
    {
        if (!string.Equals(recipe.MainImageId, selectedMainImageId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(recipe.SupportImageId, selectedSupportImageId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] requiredMinorPatterns = recipe.MinorPatternIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(NormalizeId)
            .ToArray();

        if (requiredMinorPatterns.Length != _selectedMinorPatterns.Count)
        {
            return false;
        }

        return requiredMinorPatterns.All(required =>
            _selectedMinorPatterns.Any(selected => string.Equals(selected, required, StringComparison.OrdinalIgnoreCase)));
    }

    private void SyncSerializedMinorPatterns()
    {
        _selectedMinorPatterns.Clear();

        if (selectedMinorPatternIds == null)
        {
            selectedMinorPatternIds = Array.Empty<string>();
            return;
        }

        foreach (string id in selectedMinorPatternIds)
        {
            string normalized = NormalizeId(id);
            if (!string.IsNullOrWhiteSpace(normalized) &&
                !_selectedMinorPatterns.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedMinorPatterns.Add(normalized);
            }
        }

        selectedMinorPatternIds = _selectedMinorPatterns.ToArray();
    }

    private void EnsureDefaults()
    {
        if (availableParts == null || availableParts.Length == 0)
        {
            availableParts = new[]
            {
                new MagicCirclePart("main.moon", "\uB2EC", "\uC228\uACA8\uC9D0, \uAE30\uC5B5, \uC7A0\uC7AC\uC6B0\uAE30 \uACC4\uC5F4\uC758 \uD575\uC2EC \uC18D\uC131.", MagicCirclePartType.MainImage),
                new MagicCirclePart("main.flame", "\uBD88\uAF43", "\uC5F0\uC18C, \uD30C\uAD34, \uC815\uD654 \uACC4\uC5F4\uC758 \uD575\uC2EC \uC18D\uC131.", MagicCirclePartType.MainImage),
                new MagicCirclePart("main.door", "\uBB38", "\uC774\uB3D9, \uACBD\uACC4, \uC18C\uD658 \uACC4\uC5F4\uC758 \uD575\uC2EC \uC18D\uC131.", MagicCirclePartType.MainImage),
                new MagicCirclePart("support.chain", "\uC0AC\uC2AC", "\uB300\uC0C1\uC744 \uBB36\uAC70\uB098 \uD6A8\uACFC\uB97C \uC9C0\uC18D\uC2DC\uD0A4\uB294 \uD2B9\uC131.", MagicCirclePartType.SupportImage),
                new MagicCirclePart("support.wing", "\uB0A0\uAC1C", "\uD6A8\uACFC\uB97C \uBE60\uB974\uAC8C \uC804\uD30C\uD558\uAC70\uB098 \uAC70\uB9AC\uB97C \uB118\uAC8C \uD558\uB294 \uD2B9\uC131.", MagicCirclePartType.SupportImage),
                new MagicCirclePart("support.mirror", "\uAC70\uC6B8", "\uD6A8\uACFC\uB97C \uBC18\uC0AC\uD558\uAC70\uB098 \uB300\uC0C1\uC744 \uBC14\uAFB8\uC5B4 \uC778\uC2DD\uD558\uAC8C \uD558\uB294 \uD2B9\uC131.", MagicCirclePartType.SupportImage),
                new MagicCirclePart("minor.silence", "\uCE68\uBB35\uBB38", "\uC18C\uB9AC\uB098 \uD754\uC801\uC744 \uC904\uC774\uB294 \uC138\uBD80\uC124\uC815.", MagicCirclePartType.MinorPattern),
                new MagicCirclePart("minor.blood", "\uD53C\uBB38", "\uC0DD\uCCB4, \uD608\uC5F0, \uD76C\uC0DD\uC744 \uB9E4\uAC1C\uB85C \uC0BC\uB294 \uC138\uBD80\uC124\uC815.", MagicCirclePartType.MinorPattern),
                new MagicCirclePart("minor.north", "\uBD81\uBC29\uBB38", "\uBC29\uD5A5\uACFC \uC704\uCE58\uB97C \uACE0\uC815\uD558\uB294 \uC138\uBD80\uC124\uC815.", MagicCirclePartType.MinorPattern),
                new MagicCirclePart("minor.sleep", "\uC218\uBA74\uBB38", "\uC758\uC2DD\uC744 \uB290\uB9AC\uAC8C \uB9CC\uB4E4\uAC70\uB098 \uC7A0\uC7AC\uC6B0\uB294 \uC138\uBD80\uC124\uC815.", MagicCirclePartType.MinorPattern)
            };
        }

        if (recipes == null || recipes.Length == 0)
        {
            recipes = new[]
            {
                new MagicCircleRecipe(
                    "spell.sleep_binding",
                    "\uC218\uBA74 \uAD6C\uC18D\uC220",
                    "\uB300\uC0C1\uC758 \uC758\uC2DD\uC744 \uB290\uB9AC\uAC8C \uBB36\uC5B4 \uC7A0\uC7AC\uC6B0\uB294 \uB9C8\uBC95. \uC0AC\uAC74 \uD604\uC7A5\uC5D0\uC11C\uB294 \uBC18\uD56D\uC774 \uC801\uC740 \uD53C\uD574\uC790\uB97C \uB9CC\uB4E4 \uB54C \uC4F0\uC77C \uC218 \uC788\uB2E4.",
                    "main.moon",
                    "support.chain",
                    new[] { "minor.sleep", "minor.silence" }),
                new MagicCircleRecipe(
                    "spell.blood_trace_lock",
                    "\uD608\uD754 \uCD94\uC801\uC18C",
                    "\uD53C\uB97C \uB9E4\uAC1C\uB85C \uD2B9\uC815 \uB300\uC0C1\uC758 \uC704\uCE58\uB97C \uACE0\uC815\uD558\uAC70\uB098 \uCD94\uC801\uD558\uB294 \uB9C8\uBC95.",
                    "main.door",
                    "support.chain",
                    new[] { "minor.blood", "minor.north" }),
                new MagicCircleRecipe(
                    "spell.silent_transfer",
                    "\uCE68\uBB35 \uC774\uC1A1\uC220",
                    "\uAC70\uB9AC\uB97C \uB118\uC5B4 \uB300\uC0C1\uC774\uB098 \uC0AC\uBB3C\uC744 \uC62E\uAE30\uB294 \uB9C8\uBC95. \uC18C\uB9AC\uB97C \uC904\uC774\uB294 \uBB38\uC591\uC774 \uD568\uAED8 \uC0AC\uC6A9\uB418\uC5C8\uB2E4.",
                    "main.door",
                    "support.wing",
                    new[] { "minor.silence" })
            };
        }
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
