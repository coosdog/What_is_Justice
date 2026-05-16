using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class AssistantDiscussionManager : MonoBehaviour
{
    [SerializeField] private string assistantName = "조수";
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private PlayerDispositionManager dispositionManager;

    private void Awake()
    {
        ResolveReferences();
    }

    public void StartReflection()
    {
        ResolveReferences();

        List<DialogueLine> lines = new()
        {
            new DialogueLine("나", BuildReflectionText())
        };

        investigationUI?.ShowSequence(lines);
    }

    public void StartAssistantTalk()
    {
        ResolveReferences();

        List<DialogueLine> lines = new()
        {
            new DialogueLine(assistantName, BuildAssistantSummary()),
            new DialogueLine("나", BuildPlayerResponse())
        };

        investigationUI?.ShowSequence(lines);
    }

    public string BuildReflectionText()
    {
        string disposition = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본";
        int evidenceCount = CountEvidence();
        int keywordCount = CountKeywords();

        if (evidenceCount == 0 && keywordCount == 0)
        {
            return $"{disposition} 성향으로 사건을 바라보고 있지만, 아직 정리할 단서가 없다.";
        }

        return $"{disposition} 성향으로 단서를 되짚어 본다. 현재 단서 {evidenceCount}개와 키워드 {keywordCount}개가 수사노트에 남아 있다.";
    }

    public string BuildAssistantSummary()
    {
        int evidenceCount = CountEvidence();
        int keywordCount = CountKeywords();

        if (evidenceCount == 0 && keywordCount == 0)
        {
            return "아직은 같이 맞춰볼 재료가 부족해 보여요. 먼저 눈에 띄는 장소를 조사해보죠.";
        }

        return $"지금까지 모은 단서는 {evidenceCount}개, 질문에 쓸 키워드는 {keywordCount}개예요. 이 중 NPC 반응이 달라지는 키워드부터 확인해보면 좋겠습니다.";
    }

    private string BuildPlayerResponse()
    {
        string disposition = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본";
        return $"{disposition} 성향 기준으로 놓친 강조 대상이 있는지 다시 확인해보자.";
    }

    private void ResolveReferences()
    {
        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    private int CountEvidence()
    {
        if (evidenceInventory == null)
        {
            return 0;
        }

        return evidenceInventory.Evidence.Count + evidenceInventory.CsvEvidence.Count;
    }

    private int CountKeywords()
    {
        if (evidenceInventory == null)
        {
            return 0;
        }

        return evidenceInventory.Keywords.Count + evidenceInventory.CsvKeywords.Count;
    }
}
