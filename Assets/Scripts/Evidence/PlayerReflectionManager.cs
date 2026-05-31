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
            new("\uB098", BuildReflectionText(), "player")
        };

        investigationUI?.ShowSequence(lines);
    }

    public string BuildReflectionText()
    {
        ResolveReferences();

        int evidenceCount = CountEvidence;
        int keywordCount = CountKeywords;
        int dialogueCount = dialogueLog != null ? dialogueLog.Entries.Count : 0;
        int hypothesisCount = investigationBoardManager != null ? investigationBoardManager.Hypotheses.Count : 0;
        string mode = dispositionManager != null ? dispositionManager.DisplayName : "\uAE30\uBCF8 \uAD00\uCC30";

        StringBuilder builder = new();
        builder.AppendLine($"\uD604\uC7AC \uAD00\uCC30\uBAA8\uB4DC: {mode}");
        builder.AppendLine($"\uC815\uB9AC\uB41C \uC815\uBCF4: \uB2E8\uC11C {evidenceCount}\uAC1C / \uD0A4\uC6CC\uB4DC {keywordCount}\uAC1C / \uBC1C\uC5B8 {dialogueCount}\uAC1C / \uAC00\uC124 {hypothesisCount}\uAC1C");
        builder.AppendLine();
        builder.AppendLine("[\uC0AC\uC0C9]");
        AppendReflection(builder, evidenceCount, keywordCount, dialogueCount, hypothesisCount);
        builder.AppendLine();
        builder.AppendLine("[\uB2E4\uC74C \uBC29\uD5A5]");
        AppendNextDirection(builder, evidenceCount, keywordCount, dialogueCount, hypothesisCount);
        builder.AppendLine();
        builder.AppendLine("[\uAD00\uCC30\uBAA8\uB4DC \uD78C\uD2B8]");
        AppendDispositionHint(builder, mode);
        return builder.ToString();
    }

    private void AppendReflection(StringBuilder builder, int evidenceCount, int keywordCount, int dialogueCount, int hypothesisCount)
    {
        if (evidenceCount == 0 && keywordCount == 0 && dialogueCount == 0)
        {
            builder.AppendLine("\uC544\uC9C1 \uBA38\uB9BF\uC18D\uC5D0 \uB193\uC744 \uC870\uAC01\uC774 \uC5C6\uB2E4. \uBA3C\uC800 \uB208\uC5D0 \uB744\uB294 \uC7A5\uC18C\uC640 \uC0AC\uBB3C\uC744 \uD655\uC778\uD558\uC790.");
            return;
        }

        if (keywordCount > evidenceCount)
        {
            builder.AppendLine("\uC9C8\uBB38\uD560 \uB9D0\uC740 \uC0DD\uACBC\uC9C0\uB9CC, \uADF8 \uB9D0\uC744 \uBC1B\uCCD0\uC904 \uBB3C\uC99D\uC740 \uC544\uC9C1 \uBD80\uC871\uD558\uB2E4.");
            return;
        }

        if (evidenceCount > 0 && keywordCount == 0)
        {
            builder.AppendLine("\uBB3C\uAC74\uC740 \uC190\uC5D0 \uB4E4\uC5B4\uC654\uC9C0\uB9CC, \uC544\uC9C1 \uADF8 \uBB3C\uAC74\uC774 \uC5B4\uB5A4 \uC9C8\uBB38\uC744 \uC5F4\uC5B4\uC8FC\uB294\uC9C0 \uC815\uB9AC\uB418\uC9C0 \uC54A\uC558\uB2E4.");
            return;
        }

        if (hypothesisCount == 0)
        {
            builder.AppendLine("\uB2E8\uC11C\uC640 \uB9D0\uC774 \uC313\uC5EC \uC788\uB2E4. \uC11C\uB85C \uC774\uC5B4\uBCFC \uC218 \uC788\uB294 \uC870\uAC01\uC744 \uBA3C\uC800 \uCC3E\uC544\uC57C \uD55C\uB2E4.");
            return;
        }

        builder.AppendLine("\uAC00\uC124\uC774 \uC0DD\uACBC\uB2E4. \uC774\uC81C \uC911\uC694\uD55C \uAC74 \uADF8 \uAC00\uC124\uC744 \uB204\uAD6C\uC758 \uB9D0\uB85C \uD754\uB4E4 \uC218 \uC788\uB294\uC9C0\uB2E4.");
    }

    private void AppendNextDirection(StringBuilder builder, int evidenceCount, int keywordCount, int dialogueCount, int hypothesisCount)
    {
        if (evidenceCount == 0)
        {
            builder.AppendLine("- \uC8FC\uBCC0 \uC624\uBE0C\uC81D\uD2B8\uB97C \uC870\uC0AC\uD574\uC11C \uCCAB \uB2E8\uC11C\uB97C \uD655\uBCF4\uD558\uC790.");
            return;
        }

        if (keywordCount == 0)
        {
            builder.AppendLine("- \uC5BB\uC740 \uB2E8\uC11C\uB97C \uB2E4\uC2DC \uD3BC\uCCD0\uBCF4\uACE0, \uC5F4\uB9B0 \uD0A4\uC6CC\uB4DC\uAC00 \uC788\uB294\uC9C0 \uD655\uC778\uD558\uC790.");
            return;
        }

        if (dialogueCount == 0)
        {
            builder.AppendLine("- \uC5F4\uB9B0 \uD0A4\uC6CC\uB4DC\uB85C NPC\uC5D0\uAC8C \uB9D0\uC744 \uAC78\uC5B4 \uBC18\uC751\uC744 \uBAA8\uC544\uBCF4\uC790.");
            return;
        }

        if (hypothesisCount == 0)
        {
            builder.AppendLine("- \uC218\uC0AC\uB178\uD2B8\uC758 \uB2E8\uC11C \uC5F0\uACB0\uC5D0\uC11C \uAD00\uB828 \uC788\uC5B4 \uBCF4\uC774\uB294 \uB2E8\uC11C\uC640 \uBC1C\uC5B8\uC744 \uC774\uC5B4\uBCF4\uC790.");
            return;
        }

        builder.AppendLine("- \uB9CC\uB4E4\uC5B4\uC9C4 \uAC00\uC124\uC744 \uB4E4\uACE0 \uB2E4\uC2DC \uD0D0\uBB38\uD558\uC790. \uAC19\uC740 \uC9C8\uBB38\uB3C4 \uAC00\uC124\uC774 \uC0DD\uAE30\uBA74 \uB2E4\uB978 \uBC18\uC751\uC744 \uB04C\uC5B4\uB0BC \uC218 \uC788\uB2E4.");
    }

    private static void AppendDispositionHint(StringBuilder builder, string mode)
    {
        if (mode.Contains("\uC608\uB9AC\uD55C \uC2DC\uC57C"))
        {
            builder.AppendLine("- \uC9C0\uAE08\uC740 \uC870\uC0AC\uC5D0 \uAC15\uD558\uB2E4. \uC5B4\uB450\uC6B4 \uACF3\uACFC \uC791\uC740 \uD754\uC801, \uBC30\uCE58\uAC00 \uBD88\uD3B8\uD55C \uBB3C\uAC74\uC744 \uBA3C\uC800 \uBCF4\uC790.");
            return;
        }

        if (mode.Contains("\uBBF8\uC138\uD55C \uCCAD\uAC01"))
        {
            builder.AppendLine("- \uC9C0\uAE08\uC740 \uD0D0\uBB38\uC5D0 \uAC15\uD558\uB2E4. \uB9D0\uC758 \uB0B4\uC6A9\uBCF4\uB2E4 \uB9DD\uC124\uC784, \uACE0\uB974\uC9C0 \uC54A\uC740 \uB9D0\uB05D\uC758 \uD754\uB4E4\uB9BC\uC744 \uB4E4\uC5B4\uBCF4\uC790.");
            return;
        }

        if (mode.Contains("\uCE68\uBB35\uC758 \uC751\uC2DC"))
        {
            builder.AppendLine("- \uC9C0\uAE08\uC740 \uC555\uBC15\uC5D0 \uAC15\uD558\uB2E4. \uBC84\uD2F0\uB294 \uC0C1\uB300\uC640 \uBB34\uB108\uC9C0\uB294 \uC0C1\uB300\uB97C \uAD6C\uBD84\uD574\uC11C \uC368\uC57C \uD55C\uB2E4.");
            return;
        }

        builder.AppendLine("- \uAE30\uBCF8 \uAD00\uCC30\uC740 \uC548\uC815\uC801\uC774\uB2E4. \uC544\uC9C1 \uBC29\uD5A5\uC774 \uD750\uB9B4 \uB54C \uC804\uCCB4 \uC0C1\uD669\uC744 \uB2E4\uC2DC \uC77D\uAE30 \uC88B\uB2E4.");
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

    private int CountEvidence => evidenceInventory == null ? 0 : evidenceInventory.Evidence.Count + evidenceInventory.CsvEvidence.Count;

    private int CountKeywords => evidenceInventory == null ? 0 : evidenceInventory.Keywords.Count + evidenceInventory.CsvKeywords.Count;
}
