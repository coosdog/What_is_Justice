using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class InvestigationBoardManager : MonoBehaviour
{
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private HypothesisRule[] hypothesisRules;

    private readonly List<InvestigationNode> _nodes = new();
    private readonly HashSet<string> _links = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<InvestigationHypothesis> _hypotheses = new();

    public IReadOnlyList<InvestigationNode> Nodes => _nodes;
    public IReadOnlyList<InvestigationHypothesis> Hypotheses => _hypotheses;
    public event Action Changed;

    private void Awake()
    {
        ResolveReferences();
        EnsureDefaultRules();
        RebuildNodes();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (evidenceInventory != null)
        {
            evidenceInventory.EvidenceAdded += HandleInventoryChanged;
            evidenceInventory.KeywordAdded += HandleInventoryChanged;
            evidenceInventory.CsvEvidenceAdded += HandleInventoryChanged;
            evidenceInventory.CsvKeywordAdded += HandleInventoryChanged;
        }

        if (dialogueLog != null)
        {
            dialogueLog.Changed += HandleLogChanged;
        }

        RebuildNodes();
    }

    private void OnDisable()
    {
        if (evidenceInventory != null)
        {
            evidenceInventory.EvidenceAdded -= HandleInventoryChanged;
            evidenceInventory.KeywordAdded -= HandleInventoryChanged;
            evidenceInventory.CsvEvidenceAdded -= HandleInventoryChanged;
            evidenceInventory.CsvKeywordAdded -= HandleInventoryChanged;
        }

        if (dialogueLog != null)
        {
            dialogueLog.Changed -= HandleLogChanged;
        }
    }

    public LinkResult TryConnect(string firstNodeId, string secondNodeId)
    {
        if (string.IsNullOrWhiteSpace(firstNodeId) || string.IsNullOrWhiteSpace(secondNodeId))
        {
            return LinkResult.Invalid("\uC5F0\uACB0\uD560 \uB178\uB4DC\uB97C \uC120\uD0DD\uD574\uC57C \uD569\uB2C8\uB2E4.");
        }

        if (string.Equals(firstNodeId, secondNodeId, StringComparison.OrdinalIgnoreCase))
        {
            return LinkResult.Invalid("\uAC19\uC740 \uB178\uB4DC\uB294 \uC11C\uB85C \uC5F0\uACB0\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        if (!HasNode(firstNodeId) || !HasNode(secondNodeId))
        {
            return LinkResult.Invalid("\uD604\uC7AC \uBCF4\uB4DC\uC5D0 \uC5C6\uB294 \uB178\uB4DC\uB294 \uC5F0\uACB0\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        string key = MakeLinkKey(firstNodeId, secondNodeId);
        if (!_links.Add(key))
        {
            return LinkResult.Valid("\uC774\uBBF8 \uC5F0\uACB0\uB41C \uB2E8\uC11C\uC785\uB2C8\uB2E4.");
        }

        List<InvestigationHypothesis> created = EvaluateHypotheses();
        Changed?.Invoke();

        if (created.Count > 0)
        {
            return LinkResult.Valid($"\uC0C8 \uAC00\uC124 \uC0DD\uC131: {created[0].Title}");
        }

        return LinkResult.Valid("\uB2E8\uC11C\uB97C \uC5F0\uACB0\uD588\uC2B5\uB2C8\uB2E4. \uC544\uC9C1 \uC644\uC131\uB41C \uAC00\uC124\uC740 \uC5C6\uC2B5\uB2C8\uB2E4.");
    }

    public bool HasLink(string firstNodeId, string secondNodeId)
    {
        return _links.Contains(MakeLinkKey(firstNodeId, secondNodeId));
    }

    private void RebuildNodes()
    {
        _nodes.Clear();

        if (evidenceInventory != null)
        {
            foreach (EvidenceData evidence in evidenceInventory.Evidence)
            {
                AddNode(evidence.EvidenceId, evidence.DisplayName, evidence.Description, InvestigationNodeType.Evidence);
            }

            foreach (CsvEvidenceRecord evidence in evidenceInventory.CsvEvidence)
            {
                AddNode(evidence.EvidenceId, evidence.DisplayName, evidence.Description, InvestigationNodeType.Evidence);
            }

            foreach (KeywordData keyword in evidenceInventory.Keywords)
            {
                AddNode(keyword.KeywordId, keyword.DisplayName, keyword.Description, InvestigationNodeType.Keyword);
            }

            foreach (CsvKeywordRecord keyword in evidenceInventory.CsvKeywords)
            {
                AddNode(keyword.KeywordId, keyword.DisplayName, keyword.Description, InvestigationNodeType.Keyword);
            }
        }

        if (dialogueLog != null)
        {
            foreach (DialogueLine line in dialogueLog.Entries)
            {
                if (!line.IsBoardCandidate)
                {
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(line.BoardDisplayName)
                    ? BuildDialogueDisplayName(line)
                    : line.BoardDisplayName;
                string description = string.IsNullOrWhiteSpace(line.BoardDescription)
                    ? line.Text
                    : line.BoardDescription;
                AddNode(line.BoardNodeId, displayName, description, InvestigationNodeType.Dialogue);
            }
        }

        EvaluateHypotheses();
        Changed?.Invoke();
    }

    private void AddNode(string nodeId, string displayName, string description, InvestigationNodeType type)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || HasNode(nodeId))
        {
            return;
        }

        _nodes.Add(new InvestigationNode(nodeId.Trim(), displayName, description, type));
    }

    private bool HasNode(string nodeId)
    {
        return _nodes.Any(node => string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    private List<InvestigationHypothesis> EvaluateHypotheses()
    {
        List<InvestigationHypothesis> created = new();
        EnsureDefaultRules();

        foreach (HypothesisRule rule in hypothesisRules)
        {
            if (rule == null || !rule.IsValid || HasHypothesis(rule.HypothesisId) || !IsRuleSatisfied(rule))
            {
                continue;
            }

            InvestigationHypothesis hypothesis = new(rule.HypothesisId, rule.Title, rule.Description);
            _hypotheses.Add(hypothesis);
            created.Add(hypothesis);
        }

        return created;
    }

    private bool IsRuleSatisfied(HypothesisRule rule)
    {
        if (rule.RequiredNodeIds == null || rule.RequiredNodeIds.Length < 2)
        {
            return false;
        }

        foreach (string nodeId in rule.RequiredNodeIds)
        {
            if (!HasNode(nodeId))
            {
                return false;
            }
        }

        for (int i = 0; i < rule.RequiredNodeIds.Length - 1; i++)
        {
            if (!HasLink(rule.RequiredNodeIds[i], rule.RequiredNodeIds[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasHypothesis(string hypothesisId)
    {
        return _hypotheses.Any(hypothesis => string.Equals(hypothesis.HypothesisId, hypothesisId, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureDefaultRules()
    {
        if (hypothesisRules != null && hypothesisRules.Length > 0)
        {
            return;
        }

        hypothesisRules = new[]
        {
            new HypothesisRule(
                "hypothesis.medicine_was_swapped",
                "\uAC15\uBBFC\uD601\uC758 \uC57D\uC740 \uBC14\uB00C\uC5C8\uB2E4",
                "\uAC15\uBBFC\uD601\uC740 \uC790\uC2E0\uC758 \uC57D\uC774\uB77C\uACE0 \uBBFF\uACE0 \uBCF5\uC6A9\uD588\uC9C0\uB9CC, \uC2E4\uC81C\uB85C\uB294 \uB2E4\uB978 \uC57D\uC774\uC5C8\uC744 \uAC00\uB2A5\uC131\uC774 \uC788\uB2E4.",
                new[] { "evidence.kang_private_medicine", "keyword.medicine" }),
            new HypothesisRule(
                "hypothesis.single_knife_many_hands",
                "\uD558\uB098\uC758 \uD749\uAE30, \uC5EC\uB7EC \uC190",
                "\uD749\uAE30\uB294 \uD558\uB098\uCC98\uB7FC \uBCF4\uC774\uC9C0\uB9CC \uC0C1\uCC98\uC758 \uAE4A\uC774\uC640 \uAC01\uB3C4\uAC00 \uB2EC\uB77C \uB2E8\uB3C5 \uBC94\uD589\uC73C\uB85C \uBCF4\uAE30 \uC5B4\uB835\uB2E4.",
                new[] { "evidence.single_knife", "keyword.wound" }),
            new HypothesisRule(
                "hypothesis.choi_is_planted_suspect",
                "\uCD5C\uD604\uB3C4\uB294 \uB9CC\uB4E4\uC5B4\uC9C4 \uBC94\uC778\uC774\uB2E4",
                "\uC99D\uAC70\uAC00 \uCD5C\uD604\uB3C4\uB97C \uD5A5\uD558\uC9C0\uB9CC, \uB108\uBB34 \uB179\uC2A4\uB7FD\uAC8C \uC815\uB9AC\uB418\uC5B4 \uC788\uC5B4 \uB204\uAD70\uAC00\uAC00 \uADF8\uB97C \uBC94\uC778\uC73C\uB85C \uB9CC\uB4E0 \uB4EF\uD558\uB2E4.",
                new[] { "evidence.choi_planted_clues", "keyword.planted_evidence" }),
            new HypothesisRule(
                "hypothesis.documentary_gathered_people",
                "\uB2E4\uD050\uBA58\uD130\uB9AC\uB294 \uBA85\uBD84\uC774\uC5C8\uB2E4",
                "\uBCF5\uADC0 \uB2E4\uD050\uB294 \uAC15\uBBFC\uD601\uC744 \uD604\uC7A5\uC73C\uB85C \uBD80\uB974\uACE0 \uACFC\uAC70 \uC0AC\uAC74 \uAD00\uACC4\uC790\uB4E4\uC744 \uD55C\uB370 \uBAA8\uC73C\uAE30 \uC704\uD55C \uBA85\uBD84\uC774\uC5C8\uB2E4.",
                new[] { "evidence.seo_production_note", "keyword.documentary" })
        };
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
    }

    private static string BuildDialogueDisplayName(DialogueLine line)
    {
        return string.IsNullOrWhiteSpace(line.Speaker)
            ? "\uBC1C\uC5B8"
            : $"{line.Speaker}\uC758 \uBC1C\uC5B8";
    }

    private void HandleInventoryChanged(EvidenceData _) => RebuildNodes();
    private void HandleInventoryChanged(KeywordData _) => RebuildNodes();
    private void HandleInventoryChanged(CsvEvidenceRecord _) => RebuildNodes();
    private void HandleInventoryChanged(CsvKeywordRecord _) => RebuildNodes();
    private void HandleLogChanged() => RebuildNodes();

    private static string MakeLinkKey(string firstNodeId, string secondNodeId)
    {
        string first = firstNodeId.Trim();
        string second = secondNodeId.Trim();
        return string.Compare(first, second, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{first}::{second}"
            : $"{second}::{first}";
    }
}

public enum InvestigationNodeType
{
    Evidence,
    Keyword,
    Dialogue,
    Observation,
    Hypothesis
}

public sealed class InvestigationNode
{
    public InvestigationNode(string nodeId, string displayName, string description, InvestigationNodeType type)
    {
        NodeId = nodeId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? nodeId : displayName;
        Description = description ?? string.Empty;
        Type = type;
    }

    public string NodeId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public InvestigationNodeType Type { get; }
}

public sealed class InvestigationHypothesis
{
    public InvestigationHypothesis(string hypothesisId, string title, string description)
    {
        HypothesisId = hypothesisId;
        Title = string.IsNullOrWhiteSpace(title) ? hypothesisId : title;
        Description = description ?? string.Empty;
    }

    public string HypothesisId { get; }
    public string Title { get; }
    public string Description { get; }
}

[Serializable]
public sealed class HypothesisRule
{
    [SerializeField] private string hypothesisId;
    [SerializeField] private string title;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private string[] requiredNodeIds;

    public HypothesisRule(string hypothesisId, string title, string description, string[] requiredNodeIds)
    {
        this.hypothesisId = hypothesisId;
        this.title = title;
        this.description = description;
        this.requiredNodeIds = requiredNodeIds;
    }

    public string HypothesisId => hypothesisId;
    public string Title => title;
    public string Description => description;
    public string[] RequiredNodeIds => requiredNodeIds;
    public bool IsValid => !string.IsNullOrWhiteSpace(hypothesisId) && requiredNodeIds != null && requiredNodeIds.Length >= 2;
}

public readonly struct LinkResult
{
    private LinkResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }

    public static LinkResult Valid(string message) => new(true, message);
    public static LinkResult Invalid(string message) => new(false, message);
}
