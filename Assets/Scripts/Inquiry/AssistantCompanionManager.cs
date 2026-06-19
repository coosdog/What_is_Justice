using System.Collections.Generic;
using UnityEngine;

public sealed class AssistantCompanionManager : MonoBehaviour
{
    [SerializeField] private string assistantName = "\uD560\uC544\uBC84\uC9C0 \uC62C\uBE7C\uBBF8";
    [SerializeField] private AssistantDialogueDatabase assistantDialogueDatabase;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private PlayerDispositionManager dispositionManager;
    [SerializeField] private bool rememberEvidenceAcquired = true;
    [SerializeField] private bool rememberKeywordUnlocked = true;

    private readonly List<PendingAssistantDiscovery> _pendingDiscoveries = new();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (evidenceInventory != null)
        {
            evidenceInventory.CsvEvidenceAdded += HandleCsvEvidenceAdded;
            evidenceInventory.EvidenceAdded += HandleEvidenceAdded;
            evidenceInventory.CsvKeywordAdded += HandleCsvKeywordAdded;
            evidenceInventory.KeywordAdded += HandleKeywordAdded;
        }
    }

    private void OnDisable()
    {
        if (evidenceInventory != null)
        {
            evidenceInventory.CsvEvidenceAdded -= HandleCsvEvidenceAdded;
            evidenceInventory.EvidenceAdded -= HandleEvidenceAdded;
            evidenceInventory.CsvKeywordAdded -= HandleCsvKeywordAdded;
            evidenceInventory.KeywordAdded -= HandleKeywordAdded;
        }
    }

    public bool StartManualTalk()
    {
        if (TryShowPendingDiscovery())
        {
            return true;
        }

        return ShowAssistantDialogue(AssistantDialogueTrigger.ManualTalk, "none");
    }

    public bool ReactToEvidence(string evidenceId)
    {
        return ShowAssistantDialogue(AssistantDialogueTrigger.EvidenceAcquired, evidenceId);
    }

    public bool ReactToKeyword(string keywordId)
    {
        return ShowAssistantDialogue(AssistantDialogueTrigger.KeywordUnlocked, keywordId);
    }

    private bool ShowAssistantDialogue(AssistantDialogueTrigger triggerType, string conditionId)
    {
        ResolveReferences();

        if (investigationUI == null || assistantDialogueDatabase == null)
        {
            return false;
        }

        if (investigationUI.IsVisible)
        {
            return false;
        }

        PlayerDisposition disposition = dispositionManager != null ? dispositionManager.CurrentDisposition : PlayerDisposition.Basic;
        if (!assistantDialogueDatabase.TryGetDialogue(triggerType, conditionId, disposition, out CsvAssistantDialogueRecord record))
        {
            return false;
        }

        List<DialogueLine> lines = ResolveDialogueLines(record.ResponseDialogueIds, assistantName, record.FallbackText);
        if (lines.Count == 0)
        {
            return false;
        }

        investigationUI.ShowSequence(lines);
        return true;
    }

    private List<DialogueLine> ResolveDialogueLines(IEnumerable<string> dialogueIds, string fallbackSpeaker, string fallbackText)
    {
        List<DialogueLine> lines = new();

        if (dialogueIds != null)
        {
            foreach (string dialogueId in dialogueIds)
            {
                if (dialogueDatabase != null && dialogueDatabase.TryGetEntry(dialogueId, out DialogueEntry entry))
                {
                    string speaker = string.IsNullOrWhiteSpace(entry.Speaker) ? fallbackSpeaker : entry.Speaker;
                    lines.Add(new DialogueLine(
                        speaker,
                        entry.Text,
                        entry.PortraitKey,
                        entry.Emotion,
                        entry.BoardNodeId,
                        entry.BoardDisplayName,
                        entry.BoardDescription,
                        entry.ShowPortraits,
                        entry.PortraitLayout));
                }
            }
        }

        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            lines.Add(new DialogueLine(fallbackSpeaker, fallbackText, "old_owl"));
        }

        return lines;
    }

    private void HandleEvidenceAdded(EvidenceData evidence)
    {
        if (rememberEvidenceAcquired && evidence != null)
        {
            RememberDiscovery(AssistantDialogueTrigger.EvidenceAcquired, evidence.EvidenceId);
        }
    }

    private void HandleCsvEvidenceAdded(CsvEvidenceRecord evidence)
    {
        if (rememberEvidenceAcquired && evidence != null)
        {
            RememberDiscovery(AssistantDialogueTrigger.EvidenceAcquired, evidence.EvidenceId);
        }
    }

    private void HandleKeywordAdded(KeywordData keyword)
    {
        if (rememberKeywordUnlocked && keyword != null)
        {
            RememberDiscovery(AssistantDialogueTrigger.KeywordUnlocked, keyword.KeywordId);
        }
    }

    private void HandleCsvKeywordAdded(CsvKeywordRecord keyword)
    {
        if (rememberKeywordUnlocked && keyword != null)
        {
            RememberDiscovery(AssistantDialogueTrigger.KeywordUnlocked, keyword.KeywordId);
        }
    }

    private void RememberDiscovery(AssistantDialogueTrigger triggerType, string conditionId)
    {
        if (string.IsNullOrWhiteSpace(conditionId))
        {
            return;
        }

        for (int i = 0; i < _pendingDiscoveries.Count; i++)
        {
            PendingAssistantDiscovery pending = _pendingDiscoveries[i];
            if (pending.TriggerType == triggerType && pending.ConditionId == conditionId)
            {
                return;
            }
        }

        _pendingDiscoveries.Add(new PendingAssistantDiscovery(triggerType, conditionId));
    }

    private bool TryShowPendingDiscovery()
    {
        ResolveReferences();

        if (investigationUI == null || assistantDialogueDatabase == null || investigationUI.IsVisible)
        {
            return false;
        }

        PlayerDisposition disposition = dispositionManager != null ? dispositionManager.CurrentDisposition : PlayerDisposition.Basic;
        if (TryShowPendingDiscoveryOfType(AssistantDialogueTrigger.KeywordUnlocked, disposition))
        {
            return true;
        }

        return TryShowPendingDiscoveryOfType(AssistantDialogueTrigger.EvidenceAcquired, disposition);
    }

    private bool TryShowPendingDiscoveryOfType(AssistantDialogueTrigger triggerType, PlayerDisposition disposition)
    {
        for (int i = _pendingDiscoveries.Count - 1; i >= 0; i--)
        {
            PendingAssistantDiscovery pending = _pendingDiscoveries[i];
            if (pending.TriggerType != triggerType)
            {
                continue;
            }

            if (!assistantDialogueDatabase.TryGetDialogue(pending.TriggerType, pending.ConditionId, disposition, out CsvAssistantDialogueRecord record))
            {
                continue;
            }

            _pendingDiscoveries.RemoveAt(i);
            List<DialogueLine> lines = ResolveDialogueLines(record.ResponseDialogueIds, assistantName, record.FallbackText);
            investigationUI.ShowSequence(lines);
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (assistantDialogueDatabase == null)
        {
            assistantDialogueDatabase = FindFirstObjectByType<AssistantDialogueDatabase>();
        }

        if (assistantDialogueDatabase == null)
        {
            assistantDialogueDatabase = gameObject.AddComponent<AssistantDialogueDatabase>();
        }

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }

        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    private readonly struct PendingAssistantDiscovery
    {
        public PendingAssistantDiscovery(AssistantDialogueTrigger triggerType, string conditionId)
        {
            TriggerType = triggerType;
            ConditionId = conditionId;
        }

        public AssistantDialogueTrigger TriggerType { get; }
        public string ConditionId { get; }
    }
}
