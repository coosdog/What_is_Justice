using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class AssistantDiscussionManager : MonoBehaviour
{
    [SerializeField] private string assistantName = "\uC950 \uC870\uC218";
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
            new DialogueLine("\uB098", BuildReflectionText(), "player")
        };

        investigationUI?.ShowSequence(lines);
    }

    public void StartAssistantTalk()
    {
        ResolveReferences();

        List<DialogueLine> lines = new()
        {
            new DialogueLine(assistantName, BuildAssistantSummary(), "assistant"),
            new DialogueLine("\uB098", BuildPlayerResponse(), "player")
        };

        investigationUI?.ShowSequence(lines);
    }

    public string BuildReflectionText()
    {
        string observationMode = dispositionManager != null ? dispositionManager.DisplayName : "\uAE30\uBCF8 \uAD00\uCC30";
        int evidenceCount = CountEvidence;
        int keywordCount = CountKeywords;

        if (evidenceCount == 0 && keywordCount == 0)
        {
            return $"{observationMode} \uBAA8\uB4DC\uB85C \uC0AC\uAC74\uC744 \uBC14\uB77C\uBCF4\uACE0 \uC788\uC9C0\uB9CC, \uC544\uC9C1 \uC815\uB9AC\uD560 \uB2E8\uC11C\uAC00 \uC5C6\uB2E4.";
        }

        return $"{observationMode} \uBAA8\uB4DC\uB85C \uB2E8\uC11C\uB97C \uB2E4\uC2DC \uD6D1\uC5B4\uBCF8\uB2E4. \uD604\uC7AC \uB2E8\uC11C {evidenceCount}\uAC1C\uC640 \uD0A4\uC6CC\uB4DC {keywordCount}\uAC1C\uAC00 \uC218\uC0AC\uB178\uD2B8\uC5D0 \uB0A8\uC544 \uC788\uB2E4.";
    }

    public string BuildAssistantSummary()
    {
        int evidenceCount = CountEvidence;
        int keywordCount = CountKeywords;

        if (evidenceCount == 0 && keywordCount == 0)
        {
            return "\uC544\uC9C1\uC740 \uAC19\uC774 \uB9DE\uCDB0\uBCFC \uC790\uB8CC\uAC00 \uBD80\uC871\uD574 \uBCF4\uC5EC\uC694. \uBA3C\uC800 \uB208\uC5D0 \uB744\uB294 \uC7A5\uC18C\uB97C \uC870\uC0AC\uD574\uBCF4\uC8E0.";
        }

        StringBuilder builder = new();
        builder.AppendLine($"\uC9C0\uAE08\uAE4C\uC9C0 \uBAA8\uC740 \uB2E8\uC11C\uB294 {evidenceCount}\uAC1C, \uC9C8\uBB38\uD560 \uD0A4\uC6CC\uB4DC\uB294 {keywordCount}\uAC1C\uC608\uC694.");
        builder.AppendLine();

        int hintCount = AppendCurrentHints(builder);
        if (hintCount == 0)
        {
            builder.AppendLine("\uC544\uC9C1\uC740 \uC904\uAE30\uAC00 \uC798 \uC548 \uBCF4\uC5EC\uC694. \uB2E8\uC11C\uAC00 \uC5F4\uB9B0 \uC778\uBB3C\uC5D0\uAC8C \uB2E4\uC2DC \uB9D0\uC744 \uAC78\uC5B4\uBCF4\uBA74 \uC791\uC740 \uBC18\uC751\uC774 \uB098\uC62C\uC9C0\uB3C4 \uBAB0\uB77C\uC694.");
        }

        builder.AppendLine();
        builder.AppendLine("\uC81C\uAC00 \uB2E4 \uCC3E\uC544\uB4DC\uB9AC\uC9C4 \uC54A\uC744\uAC8C\uC694. \uADF8\uB798\uB3C4 \uB9D0\uC774 \uBD88\uD3B8\uD574\uC9C0\uB294 \uC0AC\uB78C\uC740 \uAF2D \uB2E4\uC2DC \uBCF4\uC2DC\uC8E0.");
        return builder.ToString();
    }

    private string BuildPlayerResponse()
    {
        string observationMode = dispositionManager != null ? dispositionManager.DisplayName : "\uAE30\uBCF8 \uAD00\uCC30";
        return $"{observationMode} \uAE30\uC900\uC73C\uB85C \uB193\uCE5C \uAC15\uC870 \uB300\uC0C1\uC774 \uC788\uB294\uC9C0 \uB2E4\uC2DC \uD655\uC778\uD574\uBCF4\uC790.";
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

    private int CountEvidence => evidenceInventory == null ? 0 : evidenceInventory.Evidence.Count + evidenceInventory.CsvEvidence.Count;

    private int CountKeywords => evidenceInventory == null ? 0 : evidenceInventory.Keywords.Count + evidenceInventory.CsvKeywords.Count;

    private int AppendCurrentHints(StringBuilder builder)
    {
        int count = 0;

        if (HasKeyword("keyword.medicine"))
        {
            AppendHint(builder, "\uC57D \uC774\uC57C\uAE30\uB294 \uAC15\uBBFC\uD601\uC744 \uC798 \uC54C\uACE0 \uC788\uB294 \uC0AC\uB78C\uACFC, \uC57D\uC758 \uC6A9\uB7C9\uC744 \uC544\uB294 \uC0AC\uB78C\uC5D0\uAC8C \uB098\uB204\uC5B4 \uBB3C\uC5B4\uBCF4\uBA74 \uC88B\uACA0\uC5B4\uC694.");
            count++;
        }

        if (HasKeyword("keyword.wound") || HasKeyword("keyword.weapon"))
        {
            AppendHint(builder, "\uD749\uAE30\uB294 \uD558\uB098\uCC98\uB7FC \uBCF4\uC774\uB294\uB370 \uC0C1\uCC98\uAC00 \uC81C\uAC01\uAC01\uC774\uB77C\uBA74, \uC190\uC758 \uD798\uC774\uB098 \uB9DD\uC124\uC784\uC774 \uB2E4\uB978 \uC0AC\uB78C\uB4E4\uC744 \uBE44\uAD50\uD574\uBCFC \uB9CC\uD574\uC694.");
            count++;
        }

        if (HasKeyword("keyword.choi_suspect") || HasKeyword("keyword.planted_evidence"))
        {
            AppendHint(builder, "\uCD5C\uD604\uB3C4 \uCABD \uC99D\uAC70\uAC00 \uB108\uBB34 \uBC18\uB4EF\uD574\uC694. \uBC94\uC778\uC774\uB77C\uC11C \uC815\uB9AC\uB41C \uAC74\uC9C0, \uC815\uB9AC\uD574\uB450\uACE0 \uB5A0\uB098\uB824\uB294 \uAC74\uC9C0 \uB530\uC838\uBD10\uC694.");
            count++;
        }

        if (HasKeyword("keyword.documentary") || HasEvidence("evidence.seo_production_note"))
        {
            AppendHint(builder, "\uCD2C\uC601 \uACC4\uD68D\uC740 \uC0AC\uB78C\uC744 \uBAA8\uC73C\uB294 \uD551\uACC4\uAC00 \uB420 \uC218 \uC788\uC5B4\uC694. \uB204\uAC00 \uB204\uAD6C\uB97C \uC774 \uC7A5\uC18C\uB85C \uBD88\uB800\uB294\uC9C0 \uB530\uB77C\uAC00\uBCF4\uC8E0.");
            count++;
        }

        if (HasKeyword("keyword.lodge") || HasEvidence("evidence.lee_room_assignment"))
        {
            AppendHint(builder, "\uBC29 \uBC30\uC815\uACFC \uBCF5\uB3C4 \uC548\uB0B4\uB294 \uBCC4\uAC70 \uC544\uB2CC \uCC99\uD558\uAE30 \uC26C\uC6CC\uC694. \uAE38\uC744 \uC544\uB294 \uC0AC\uB78C\uC774 \uB9CC\uB4E0 \uC6B0\uC5F0\uC740 \uC6B0\uC5F0\uC774 \uC544\uB2D0 \uC218\uB3C4 \uC788\uACE0\uC694.");
            count++;
        }

        if (HasKeyword("keyword.victim") || HasKeyword("keyword.accident"))
        {
            AppendHint(builder, "5\uB144 \uC804 \uC0AC\uACE0 \uC774\uC57C\uAE30\uB294 \uB204\uAD6C\uC5D0\uAC8C\uB098 \uAC19\uC740 \uC0C1\uCC98\uAC00 \uC544\uB2C8\uC5D0\uC694. \uBD84\uB178\uBCF4\uB2E4 \uC8C4\uCC45\uAC10\uC774 \uBA3C\uC800 \uB098\uC624\uB294 \uC0AC\uB78C\uC744 \uBCF4\uC138\uC694.");
            count++;
        }

        if (HasKeyword("keyword.alibi"))
        {
            AppendHint(builder, "\uC54C\uB9AC\uBC14\uC774\uB294 \uC11C\uB85C \uB9DE\uC544\uB5A8\uC5B4\uC9C8\uC218\uB85D \uC5F0\uC2B5\uD55C \uB9D0\uCC98\uB7FC \uB4E4\uB9B4 \uB54C\uAC00 \uC788\uC5B4\uC694.");
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
