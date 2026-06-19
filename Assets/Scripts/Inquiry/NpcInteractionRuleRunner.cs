using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NpcInteractionRuleRunner : MonoBehaviour
{
    [SerializeField] private NpcInteractionRuleDatabase ruleDatabase;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private InvestigationUI investigationUI;

    private readonly HashSet<string> _completedRunOnceRules = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        ResolveReferences();
    }

    public bool TryRun(string npcId, string action)
    {
        ResolveReferences();

        if (ruleDatabase == null)
        {
            return false;
        }

        IReadOnlyList<CsvNpcInteractionRuleRecord> rules = ruleDatabase.GetRules(npcId, action);
        for (int i = 0; i < rules.Count; i++)
        {
            CsvNpcInteractionRuleRecord rule = rules[i];
            if (rule.RunOnce && _completedRunOnceRules.Contains(rule.RuleId))
            {
                continue;
            }

            if (!MatchesCondition(rule))
            {
                continue;
            }

            if (!ExecuteResult(rule))
            {
                continue;
            }

            if (rule.RunOnce)
            {
                _completedRunOnceRules.Add(rule.RuleId);
            }

            return true;
        }

        return false;
    }

    private bool MatchesCondition(CsvNpcInteractionRuleRecord rule)
    {
        string conditionType = Normalize(rule.ConditionType);
        return conditionType switch
        {
            "always" => true,
            "hasitem" => inventoryManager != null && inventoryManager.HasItem(rule.ConditionValue),
            "missingitem" => inventoryManager == null || !inventoryManager.HasItem(rule.ConditionValue),
            _ => false
        };
    }

    private bool ExecuteResult(CsvNpcInteractionRuleRecord rule)
    {
        string resultType = Normalize(rule.ResultType);
        switch (resultType)
        {
            case "dialogue":
                return ShowDialogue(rule.ResultValue);
            case "event":
                GameEventBus.Raise(rule.ResultValue);
                return true;
            default:
                Debug.LogWarning($"Unsupported NPC interaction result type '{rule.ResultType}' on rule '{rule.RuleId}'.");
                return false;
        }
    }

    private bool ShowDialogue(string dialogueId)
    {
        if (dialogueDatabase == null || investigationUI == null ||
            !dialogueDatabase.TryGetEntry(dialogueId, out DialogueEntry entry))
        {
            Debug.LogWarning($"NPC interaction dialogue '{dialogueId}' could not be shown.");
            return false;
        }

        investigationUI.ShowSequence(new[]
        {
            new DialogueLine(
                entry.Speaker,
                entry.Text,
                entry.PortraitKey,
                entry.Emotion,
                entry.BoardNodeId,
                entry.BoardDisplayName,
                entry.BoardDescription,
                entry.ShowPortraits,
                entry.PortraitLayout)
        });
        return true;
    }

    private void ResolveReferences()
    {
        if (ruleDatabase == null)
        {
            ruleDatabase = FindFirstObjectByType<NpcInteractionRuleDatabase>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }
}
