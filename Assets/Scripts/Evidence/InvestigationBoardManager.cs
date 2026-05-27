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
            return LinkResult.Invalid("연결할 노드를 선택해야 한다.");
        }

        if (string.Equals(firstNodeId, secondNodeId, StringComparison.OrdinalIgnoreCase))
        {
            return LinkResult.Invalid("같은 노드는 서로 연결할 수 없다.");
        }

        if (!HasNode(firstNodeId) || !HasNode(secondNodeId))
        {
            return LinkResult.Invalid("현재 보드에 없는 노드는 연결할 수 없다.");
        }

        string key = MakeLinkKey(firstNodeId, secondNodeId);
        if (!_links.Add(key))
        {
            return LinkResult.Valid("이미 연결된 단서다.");
        }

        List<InvestigationHypothesis> created = EvaluateHypotheses();
        Changed?.Invoke();

        if (created.Count > 0)
        {
            return LinkResult.Valid($"새 가설 생성: {created[0].Title}");
        }

        return LinkResult.Valid("두 단서를 연결했다. 아직 완성된 가설은 없다.");
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
            for (int i = 0; i < dialogueLog.Entries.Count; i++)
            {
                DialogueLine line = dialogueLog.Entries[i];
                string label = string.IsNullOrWhiteSpace(line.Speaker) ? "발언" : $"{line.Speaker}의 발언";
                AddNode($"dialogue.{i:000}", label, line.Text, InvestigationNodeType.Dialogue);
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
                "강민혁의 약은 바뀌었다",
                "강민혁은 남이 건넨 약을 먹은 것이 아니라, 자기 약이라고 믿은 것을 직접 복용했다.",
                new[] { "evidence.kang_private_medicine", "keyword.medicine" }),
            new HypothesisRule(
                "hypothesis.single_knife_many_hands",
                "하나의 흉기, 여러 손",
                "흉기는 하나처럼 보이지만 자상의 깊이와 각도가 달라 단독 범행으로 보기 어렵다.",
                new[] { "evidence.single_knife", "keyword.wound" }),
            new HypothesisRule(
                "hypothesis.choi_is_planted_suspect",
                "최현도는 만들어진 범인이다",
                "증거가 최현도를 향하지만 너무 완벽하게 정리되어 있어 누군가가 그를 범인으로 만들려 한 정황이 보인다.",
                new[] { "evidence.choi_planted_clues", "keyword.planted_evidence" }),
            new HypothesisRule(
                "hypothesis.documentary_gathered_people",
                "다큐는 미끼였다",
                "복귀 다큐는 강민혁을 산장으로 부르고 과거 사건 관계자들을 한곳에 모으기 위한 명분이었다.",
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

    public static LinkResult Valid(string message) => new LinkResult(true, message);
    public static LinkResult Invalid(string message) => new LinkResult(false, message);
}
