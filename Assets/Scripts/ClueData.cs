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

    [Header("Outcomes")]
    [SerializeField] private EvidenceData rewardEvidence;
    [SerializeField] private BackgroundData nextBackground;
    [SerializeField] private bool runOutcomesOnlyOnFirstInvestigation = true;

    public string ClueId => clueId;
    public string DisplayName => displayName;
    public string FirstInvestigationText => firstInvestigationText;
    public string AlreadyInvestigatedText => alreadyInvestigatedText;
    public EvidenceData RewardEvidence => rewardEvidence;
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
