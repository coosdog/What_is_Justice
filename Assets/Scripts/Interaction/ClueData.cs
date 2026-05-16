using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClueData", menuName = "Game/Investigation/Clue Data")]
public sealed class ClueData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string clueId;
    [SerializeField] private string displayName;

    [Header("CSV Dialogue Sequences")]
    [SerializeField] private string[] firstInvestigationDialogueIds;
    [SerializeField] private string[] alreadyInvestigatedDialogueIds;

    [Header("Fallback Texts")]
    [TextArea(2, 5)]
    [SerializeField] private string firstInvestigationText;

    [TextArea(2, 5)]
    [SerializeField] private string alreadyInvestigatedText = "이미 조사한 대상이다.";

    [Header("Disposition Overrides")]
    [SerializeField] private DispositionDialogueOverride[] dispositionOverrides;

    [Header("Outcomes")]
    [SerializeField] private EvidenceData rewardEvidence;
    [SerializeField] private string rewardEvidenceId;
    [SerializeField] private BackgroundData nextBackground;
    [SerializeField] private bool runOutcomesOnlyOnFirstInvestigation = true;

    public string ClueId => clueId;
    public string DisplayName => displayName;
    public string FirstInvestigationText => firstInvestigationText;
    public string AlreadyInvestigatedText => alreadyInvestigatedText;
    public EvidenceData RewardEvidence => rewardEvidence;
    public string RewardEvidenceId => rewardEvidenceId;
    public BackgroundData NextBackground => nextBackground;
    public bool RunOutcomesOnlyOnFirstInvestigation => runOutcomesOnlyOnFirstInvestigation;

    public IEnumerable<string> EnumerateFirstInvestigationDialogueIds()
    {
        return EnumerateDialogueIds(firstInvestigationDialogueIds);
    }

    public IEnumerable<string> EnumerateAlreadyInvestigatedDialogueIds()
    {
        return EnumerateDialogueIds(alreadyInvestigatedDialogueIds);
    }

    public IEnumerable<string> EnumerateFirstInvestigationDialogueIds(PlayerDisposition disposition)
    {
        return TryGetDispositionOverride(disposition, out DispositionDialogueOverride dialogueOverride) &&
               dialogueOverride.HasFirstInvestigationDialogueIds
            ? dialogueOverride.EnumerateFirstInvestigationDialogueIds()
            : EnumerateFirstInvestigationDialogueIds();
    }

    public IEnumerable<string> EnumerateAlreadyInvestigatedDialogueIds(PlayerDisposition disposition)
    {
        return TryGetDispositionOverride(disposition, out DispositionDialogueOverride dialogueOverride) &&
               dialogueOverride.HasAlreadyInvestigatedDialogueIds
            ? dialogueOverride.EnumerateAlreadyInvestigatedDialogueIds()
            : EnumerateAlreadyInvestigatedDialogueIds();
    }

    public string GetFirstInvestigationText(PlayerDisposition disposition)
    {
        return TryGetDispositionOverride(disposition, out DispositionDialogueOverride dialogueOverride) &&
               !string.IsNullOrWhiteSpace(dialogueOverride.FirstInvestigationText)
            ? dialogueOverride.FirstInvestigationText
            : firstInvestigationText;
    }

    public string GetAlreadyInvestigatedText(PlayerDisposition disposition)
    {
        return TryGetDispositionOverride(disposition, out DispositionDialogueOverride dialogueOverride) &&
               !string.IsNullOrWhiteSpace(dialogueOverride.AlreadyInvestigatedText)
            ? dialogueOverride.AlreadyInvestigatedText
            : alreadyInvestigatedText;
    }

    private bool TryGetDispositionOverride(PlayerDisposition disposition, out DispositionDialogueOverride dialogueOverride)
    {
        if (dispositionOverrides != null)
        {
            foreach (DispositionDialogueOverride candidate in dispositionOverrides)
            {
                if (candidate != null && candidate.Disposition == disposition)
                {
                    dialogueOverride = candidate;
                    return true;
                }
            }
        }

        dialogueOverride = null;
        return false;
    }

    private static IEnumerable<string> EnumerateDialogueIds(string[] ids)
    {
        if (ids == null)
        {
            yield break;
        }

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                yield return id;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(clueId))
        {
            clueId = name;
        }
    }
#endif
}

[System.Serializable]
public sealed class DispositionDialogueOverride
{
    [SerializeField] private PlayerDisposition disposition;
    [SerializeField] private string[] firstInvestigationDialogueIds;
    [SerializeField] private string[] alreadyInvestigatedDialogueIds;

    [TextArea(2, 5)]
    [SerializeField] private string firstInvestigationText;

    [TextArea(2, 5)]
    [SerializeField] private string alreadyInvestigatedText;

    public PlayerDisposition Disposition => disposition;
    public string FirstInvestigationText => firstInvestigationText;
    public string AlreadyInvestigatedText => alreadyInvestigatedText;
    public bool HasFirstInvestigationDialogueIds => HasAny(firstInvestigationDialogueIds);
    public bool HasAlreadyInvestigatedDialogueIds => HasAny(alreadyInvestigatedDialogueIds);

    public IEnumerable<string> EnumerateFirstInvestigationDialogueIds()
    {
        return EnumerateDialogueIds(firstInvestigationDialogueIds);
    }

    public IEnumerable<string> EnumerateAlreadyInvestigatedDialogueIds()
    {
        return EnumerateDialogueIds(alreadyInvestigatedDialogueIds);
    }

    private static bool HasAny(string[] values)
    {
        if (values == null)
        {
            return false;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateDialogueIds(string[] ids)
    {
        if (ids == null)
        {
            yield break;
        }

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                yield return id;
            }
        }
    }
}

