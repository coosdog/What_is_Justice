using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class AssistantDiscussionManager : MonoBehaviour
{
    [SerializeField] private string assistantName = "쥐 조수";
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
            new DialogueLine("나", BuildReflectionText(), "player")
        };

        investigationUI?.ShowSequence(lines);
    }

    public void StartAssistantTalk()
    {
        ResolveReferences();

        List<DialogueLine> lines = new()
        {
            new DialogueLine(assistantName, BuildAssistantSummary(), "assistant"),
            new DialogueLine("나", BuildPlayerResponse(), "player")
        };

        investigationUI?.ShowSequence(lines);
    }

    public string BuildReflectionText()
    {
        string observationMode = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본 관찰";
        int evidenceCount = CountEvidence();
        int keywordCount = CountKeywords();

        if (evidenceCount == 0 && keywordCount == 0)
        {
            return $"{observationMode} 모드로 사건을 바라보고 있지만, 아직 정리할 단서가 없다.";
        }

        return $"{observationMode} 모드로 단서를 되짚어 본다. 현재 단서 {evidenceCount}개와 키워드 {keywordCount}개가 수사노트에 남아 있다.";
    }

    public string BuildAssistantSummary()
    {
        int evidenceCount = CountEvidence();
        int keywordCount = CountKeywords();

        if (evidenceCount == 0 && keywordCount == 0)
        {
            return "아직은 같이 맞춰볼 자료가 부족해 보여요. 먼저 눈에 띄는 장소를 조사해보죠.";
        }

        StringBuilder builder = new();
        builder.AppendLine($"지금까지 모은 단서는 {evidenceCount}개, 질문할 키워드는 {keywordCount}개예요.");
        builder.AppendLine();

        int hintCount = AppendCurrentHints(builder);
        if (hintCount == 0)
        {
            builder.AppendLine("아직은 큰 줄기가 잘 안 보여요. 단서가 열린 인물에게 다시 말을 걸어보면 숨은 반응이 나올지도 몰라요.");
        }

        builder.AppendLine();
        builder.AppendLine("제가 딱 찍어드리진 않을게요. 그래도 말이 어긋나는 사람은 꼭 다시 보죠.");
        return builder.ToString();
    }

    private string BuildPlayerResponse()
    {
        string observationMode = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본 관찰";
        return $"{observationMode} 기준으로 놓친 강조 대상이 있는지 다시 확인해보자.";
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

    private int AppendCurrentHints(StringBuilder builder)
    {
        int count = 0;

        if (HasKeyword("keyword.medicine"))
        {
            AppendHint(builder, "약 이야기는 강민혁을 잘 아는 사람과, 약의 용량을 아는 사람을 나눠서 물어보면 좋겠어요.");
            count++;
        }

        if (HasKeyword("keyword.wound") || HasKeyword("keyword.weapon"))
        {
            AppendHint(builder, "칼은 하나인데 상처가 제각각이라면, 손의 힘이나 망설임이 다른 사람들을 비교해봐야겠죠.");
            count++;
        }

        if (HasKeyword("keyword.choi_suspect") || HasKeyword("keyword.planted_evidence"))
        {
            AppendHint(builder, "최현도 씨 쪽 증거는 너무 반듯해요. 범인이라서 남긴 건지, 남기고 싶어서 둔 건지 따져봐요.");
            count++;
        }

        if (HasKeyword("keyword.documentary") || HasEvidence("evidence.seo_production_note"))
        {
            AppendHint(builder, "촬영 계획은 사람을 모으는 핑계가 될 수 있어요. 누가 누구를 이 산장으로 불렀는지 살펴보죠.");
            count++;
        }

        if (HasKeyword("keyword.lodge") || HasEvidence("evidence.lee_room_assignment"))
        {
            AppendHint(builder, "방 배정과 복도 안내는 별거 아닌 척하기 쉬워요. 길을 아는 사람이 만든 우연은 우연이 아닐 수도 있고요.");
            count++;
        }

        if (HasKeyword("keyword.victim") || HasKeyword("keyword.accident"))
        {
            AppendHint(builder, "5년 전 사고 이야기는 누구에게나 같은 상처가 아니에요. 분노보다 죄책감이 먼저 나오는 사람을 봐요.");
            count++;
        }

        if (HasKeyword("keyword.alibi"))
        {
            AppendHint(builder, "알리바이는 서로 맞아떨어질수록 오히려 준비된 말처럼 들릴 때가 있어요.");
            count++;
        }

        return count;
    }

    private static void AppendHint(StringBuilder builder, string text)
    {
        builder.AppendLine($"- {text}");
    }

    private bool HasEvidence(string evidenceId)
    {
        return evidenceInventory != null && evidenceInventory.HasEvidence(evidenceId);
    }

    private bool HasKeyword(string keywordId)
    {
        return evidenceInventory != null && evidenceInventory.HasKeyword(keywordId);
    }
}
