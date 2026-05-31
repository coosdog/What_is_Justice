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
        if (!PlayerDispositionManager.IsInquiryMode(disposition))
        {
            ShowDialogue(null, npcData.DisplayName, GetInquiryMismatchText(disposition));
            return;
        }

        if (csvInvestigationDatabase != null && csvInvestigationDatabase.TryGetNpcTopic(npcData.NpcId, keyword.KeywordId, disposition, out CsvNpcInquiryTopicRecord topic))
        {
            if (RequiresSpecificInquiryResponse(disposition) && IsBasicTopic(topic))
            {
                ShowDialogue(null, npcData.DisplayName, GetIneffectiveInquiryText(disposition));
                return;
            }

            ShowDialogue(topic.ResponseDialogueIds, npcData.DisplayName, topic.FallbackResponseText);
        }
        else
        {
            ShowDialogue(npcData.UnknownKeywordDialogueIds, npcData.DisplayName, npcData.UnknownKeywordFallbackText);
        }
    }

    private static bool RequiresSpecificInquiryResponse(PlayerDisposition disposition)
    {
        return disposition == PlayerDisposition.Tendency2 ||
               disposition == PlayerDisposition.Tendency3;
    }

    private static bool IsBasicTopic(CsvNpcInquiryTopicRecord topic)
    {
        return topic == null ||
               string.IsNullOrWhiteSpace(topic.Disposition) ||
               string.Equals(topic.Disposition, "basic", System.StringComparison.OrdinalIgnoreCase);
    }

    private static string GetIneffectiveInquiryText(PlayerDisposition disposition)
    {
        return disposition switch
        {
            PlayerDisposition.Tendency2 => "귀를 기울였지만, 상대의 말에서는 더 읽어낼 흔들림이 없다. 이 질문에는 미세한 청각이 잘 맞지 않는다.",
            PlayerDisposition.Tendency3 => "시선을 고정했지만, 상대는 쉽게 무너지지 않는다. 이 질문에는 침묵의 응시가 통하지 않는다.",
            _ => string.Empty
        };
    }

    private static string GetInquiryMismatchText(PlayerDisposition disposition)
    {
        return disposition switch
        {
            PlayerDisposition.Tendency1 => "예리한 시야로 상대의 표정과 자세는 살필 수 있지만, 말의 빈틈은 다른 방식으로 파고들어야 한다.",
            _ => string.Empty
        };
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
            lines.Add(new DialogueLine(fallbackSpeaker, fallbackText));
        }

        return lines;
    }
}
