using System.Collections.Generic;
using UnityEngine;

public sealed class NpcInquiryManager : MonoBehaviour
{
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private CsvInvestigationDatabase csvInvestigationDatabase;
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private KeywordSelectionUI keywordSelectionUI;
    [SerializeField] private PlayerDispositionManager dispositionManager;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        if (csvInvestigationDatabase == null)
        {
            csvInvestigationDatabase = FindFirstObjectByType<CsvInvestigationDatabase>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }

        if (keywordSelectionUI == null)
        {
            keywordSelectionUI = FindFirstObjectByType<KeywordSelectionUI>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    public void StartInquiry(NpcInquiryData npcData)
    {
        ResolveReferences();

        if (npcData == null || investigationUI == null || IsBusy())
        {
            return;
        }

        if (evidenceInventory == null || !evidenceInventory.HasAnyKeyword)
        {
            ShowDialogue(npcData.EnumerateNoKeywordDialogueIds(), npcData.DisplayName, npcData.NoKeywordFallbackText);
            return;
        }

        if (keywordSelectionUI == null)
        {
            ShowDialogue(npcData.EnumerateUnknownKeywordDialogueIds(), npcData.DisplayName, npcData.UnknownKeywordFallbackText);
            return;
        }

        if (evidenceInventory.Keywords.Count == 0)
        {
            ShowDialogue(npcData.EnumerateNoKeywordDialogueIds(), npcData.DisplayName, npcData.NoKeywordFallbackText);
            return;
        }

        keywordSelectionUI.Show($"{npcData.DisplayName}에게 무엇을 물어볼까?", evidenceInventory.Keywords, keyword => HandleKeywordSelected(npcData, keyword));
    }

    public void StartInquiry(string npcId)
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(npcId) || investigationUI == null || IsBusy())
        {
            return;
        }

        if (csvInvestigationDatabase == null || !csvInvestigationDatabase.TryGetNpcInquiry(npcId, out CsvNpcInquiryRecord npcData))
        {
            Debug.LogWarning($"CSV NPC inquiry not found: {npcId}");
            return;
        }

        if (evidenceInventory == null || !evidenceInventory.HasAnyKeyword)
        {
            ShowDialogue(npcData.NoKeywordDialogueIds, npcData.DisplayName, npcData.NoKeywordFallbackText);
            return;
        }

        if (keywordSelectionUI == null)
        {
            ShowDialogue(npcData.UnknownKeywordDialogueIds, npcData.DisplayName, npcData.UnknownKeywordFallbackText);
            return;
        }

        if (evidenceInventory.CsvKeywords.Count == 0)
        {
            ShowDialogue(npcData.NoKeywordDialogueIds, npcData.DisplayName, npcData.NoKeywordFallbackText);
            return;
        }

        keywordSelectionUI.ShowCsv($"{npcData.DisplayName}에게 무엇을 물어볼까?", evidenceInventory.CsvKeywords, keyword => HandleCsvKeywordSelected(npcData, keyword));
    }

    private bool IsBusy()
    {
        return investigationUI != null && investigationUI.IsVisible ||
               keywordSelectionUI != null && keywordSelectionUI.IsVisible;
    }

    private void HandleKeywordSelected(NpcInquiryData npcData, KeywordData keyword)
    {
        if (npcData == null || keyword == null)
        {
            return;
        }

        if (npcData.TryGetTopic(keyword, out NpcInquiryTopic topic))
        {
            ShowDialogue(topic.EnumerateResponseDialogueIds(), npcData.DisplayName, topic.FallbackResponseText);
        }
        else
        {
            ShowDialogue(npcData.EnumerateUnknownKeywordDialogueIds(), npcData.DisplayName, npcData.UnknownKeywordFallbackText);
        }
    }

    private void HandleCsvKeywordSelected(CsvNpcInquiryRecord npcData, CsvKeywordRecord keyword)
    {
        if (npcData == null || keyword == null)
        {
            return;
        }

        PlayerDisposition disposition = dispositionManager != null ? dispositionManager.CurrentDisposition : PlayerDisposition.Basic;
        if (csvInvestigationDatabase != null && csvInvestigationDatabase.TryGetNpcTopic(npcData.NpcId, keyword.KeywordId, disposition, out CsvNpcInquiryTopicRecord topic))
        {
            ShowDialogue(topic.ResponseDialogueIds, npcData.DisplayName, topic.FallbackResponseText);
        }
        else
        {
            ShowDialogue(npcData.UnknownKeywordDialogueIds, npcData.DisplayName, npcData.UnknownKeywordFallbackText);
        }
    }

    private void ShowDialogue(IEnumerable<string> dialogueIds, string fallbackSpeaker, string fallbackText)
    {
        if (investigationUI == null)
        {
            return;
        }

        List<DialogueLine> lines = ResolveDialogueLines(dialogueIds, fallbackSpeaker, fallbackText);
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
}
