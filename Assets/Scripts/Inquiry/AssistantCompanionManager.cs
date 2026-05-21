using System.Collections.Generic;
using UnityEngine;

public sealed class AssistantCompanionManager : MonoBehaviour
{
    [SerializeField] private string assistantName = "\uC950 \uC870\uC218";
    [SerializeField] private AssistantDialogueDatabase assistantDialogueDatabase;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private PlayerDispositionManager dispositionManager;
    [SerializeField] private bool reactToEvidenceAcquired = true;
    [SerializeField] private bool reactToKeywordUnlocked;

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

    public void StartManualTalk()
    {
        ShowAssistantDialogue(AssistantDialogueTrigger.ManualTalk, "none");
    }

    public void ReactToEvidence(string evidenceId)
    {
        ShowAssistantDialogue(AssistantDialogueTrigger.EvidenceAcquired, evidenceId);
    }

    public void ReactToKeyword(string keywordId)
    {
        ShowAssistantDialogue(AssistantDialogueTrigger.KeywordUnlocked, keywordId);
    }

    private void ShowAssistantDialogue(AssistantDialogueTrigger triggerType, string conditionId)
    {
        ResolveReferences();

        if (investigationUI == null || assistantDialogueDatabase == null)
        {
            return;
        }

        if (investigationUI.IsVisible)
        {
            return;
        }

        PlayerDisposition disposition = dispositionManager != null ? dispositionManager.CurrentDisposition : PlayerDisposition.Basic;
        if (!assistantDialogueDatabase.TryGetDialogue(triggerType, conditionId, disposition, out CsvAssistantDialogueRecord record))
        {
            return;
        }

        List<DialogueLine> lines = ResolveDialogueLines(record.ResponseDialogueIds, assistantName, record.FallbackText);
        investigationUI.ShowSequence(lines);
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
                    lines.Add(new DialogueLine(speaker, entry.Text));
                }
            }
        }

        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            lines.Add(new DialogueLine(fallbackSpeaker, fallbackText));
        }

        return lines;
    }

    private void HandleEvidenceAdded(EvidenceData evidence)
    {
        if (reactToEvidenceAcquired && evidence != null)
        {
            ReactToEvidence(evidence.EvidenceId);
        }
    }

    private void HandleCsvEvidenceAdded(CsvEvidenceRecord evidence)
    {
        if (reactToEvidenceAcquired && evidence != null)
        {
            ReactToEvidence(evidence.EvidenceId);
        }
    }

    private void HandleKeywordAdded(KeywordData keyword)
    {
        if (reactToKeywordUnlocked && keyword != null)
        {
            ReactToKeyword(keyword.KeywordId);
        }
    }

    private void HandleCsvKeywordAdded(CsvKeywordRecord keyword)
    {
        if (reactToKeywordUnlocked && keyword != null)
        {
            ReactToKeyword(keyword.KeywordId);
        }
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
}
