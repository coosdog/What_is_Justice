using System.Text;
using UnityEngine;

public sealed class PlayerReflectionManager : MonoBehaviour
{
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private InvestigationBoardManager investigationBoardManager;
    [SerializeField] private PlayerDispositionManager dispositionManager;
    [SerializeField] private InvestigationUI investigationUI;

    private void Awake()
    {
        ResolveReferences();
    }

    public void StartReflection()
    {
        ResolveReferences();

        DialogueLine[] lines =
        {
            new DialogueLine("나", BuildReflectionText(), "player")
        };

        investigationUI?.ShowSequence(lines);
    }

    public string BuildReflectionText()
    {
        ResolveReferences();

        int evidenceCount = CountEvidence();
        int keywordCount = CountKeywords();
        int dialogueCount = dialogueLog != null ? dialogueLog.Entries.Count : 0;
        int hypothesisCount = investigationBoardManager != null ? investigationBoardManager.Hypotheses.Count : 0;
        string mode = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본 관찰";

        StringBuilder builder = new();
        builder.AppendLine($"현재 관찰모드: {mode}");
        builder.AppendLine($"정리된 정보: 단서 {evidenceCount}개 / 키워드 {keywordCount}개 / 발언 {dialogueCount}개 / 가설 {hypothesisCount}개");
        builder.AppendLine();
        builder.AppendLine("[사색]");
        AppendReflection(builder, evidenceCount, keywordCount, dialogueCount, hypothesisCount);
        builder.AppendLine();
        builder.AppendLine("[다음 방향]");
        AppendNextDirection(builder, evidenceCount, keywordCount, dialogueCount, hypothesisCount);
        builder.AppendLine();
        builder.AppendLine("[관찰모드 단서]");
        AppendDispositionHint(builder, mode);
        return builder.ToString();
    }

    private void AppendReflection(StringBuilder builder, int evidenceCount, int keywordCount, int dialogueCount, int hypothesisCount)
    {
        if (evidenceCount == 0 && keywordCount == 0 && dialogueCount == 0)
        {
            builder.AppendLine("아직 머릿속에 놓을 조각이 없다. 먼저 눈에 띄는 장소와 사물을 훑어야 한다.");
            return;
        }

        if (keywordCount > evidenceCount)
        {
            builder.AppendLine("질문할 말은 늘었지만, 그 말을 받쳐줄 물증은 아직 얇다.");
            return;
        }

        if (evidenceCount > 0 && keywordCount == 0)
        {
            builder.AppendLine("물건은 손에 들어왔지만, 아직 그 물건이 어떤 질문을 열어주는지 정리되지 않았다.");
            return;
        }

        if (hypothesisCount == 0)
        {
            builder.AppendLine("단서와 말은 흩어져 있다. 서로 닿아 있는 조각을 먼저 찾아야 한다.");
            return;
        }

        builder.AppendLine("가설은 생겼다. 이제 중요한 건 그 가설을 누구의 말로 흔들 수 있는지다.");
    }

    private void AppendNextDirection(StringBuilder builder, int evidenceCount, int keywordCount, int dialogueCount, int hypothesisCount)
    {
        if (evidenceCount == 0)
        {
            builder.AppendLine("- 주변 오브젝트를 조사해서 첫 단서를 확보하자.");
            return;
        }

        if (keywordCount == 0)
        {
            builder.AppendLine("- 획득한 단서를 다시 살펴보고, 열리는 키워드가 있는지 확인하자.");
            return;
        }

        if (dialogueCount == 0)
        {
            builder.AppendLine("- 열린 키워드로 NPC에게 말을 걸어 반응을 모아보자.");
            return;
        }

        if (hypothesisCount == 0)
        {
            builder.AppendLine("- 수사노트의 단서 연결에서 관련 있어 보이는 단서와 발언을 이어보자.");
            return;
        }

        builder.AppendLine("- 만들어진 가설을 들고 다시 탐문하자. 같은 질문도 가설이 생기면 다른 반응을 끌어낼 수 있다.");
    }

    private static void AppendDispositionHint(StringBuilder builder, string mode)
    {
        if (mode.Contains("예리한 시야"))
        {
            builder.AppendLine("- 지금은 조사에 강하다. 어두운 곳, 작은 흔적, 배치가 어긋난 물건을 먼저 보자.");
            return;
        }

        if (mode.Contains("미세한 청각"))
        {
            builder.AppendLine("- 지금은 탐문에 강하다. 말의 내용보다 망설임, 숨 고르기, 말끝의 흔들림을 들어보자.");
            return;
        }

        if (mode.Contains("침묵의 응시"))
        {
            builder.AppendLine("- 지금은 압박에 강하다. 버티는 상대와 무너지는 상대를 구분해서 써야 한다.");
            return;
        }

        builder.AppendLine("- 기본 관찰은 안정적이다. 아직 방향이 흐릴 때 전체 상황을 다시 훑기 좋다.");
    }

    private void ResolveReferences()
    {
        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (dialogueLog == null)
        {
            dialogueLog = FindFirstObjectByType<DialogueLog>();
        }

        if (investigationBoardManager == null)
        {
            investigationBoardManager = FindFirstObjectByType<InvestigationBoardManager>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
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
