using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EvidenceInventory : MonoBehaviour
{
    [SerializeField] private EvidenceData[] startingEvidence;

    public event Action<EvidenceData> EvidenceAdded;

    private readonly Dictionary<string, EvidenceData> _evidenceById = new();

    public IReadOnlyCollection<EvidenceData> Evidence => _evidenceById.Values;

    private void Awake()
    {
        if (startingEvidence == null)
        {
            return;
        }

        foreach (EvidenceData evidence in startingEvidence)
        {
            AddEvidence(evidence, false);
        }
    }

    public bool HasEvidence(EvidenceData evidence)
    {
        return evidence != null && HasEvidence(evidence.EvidenceId);
    }

    public bool HasEvidence(string evidenceId)
    {
        return !string.IsNullOrWhiteSpace(evidenceId) && _evidenceById.ContainsKey(evidenceId);
    }

    public bool AddEvidence(EvidenceData evidence)
    {
        return AddEvidence(evidence, true);
    }

    private bool AddEvidence(EvidenceData evidence, bool notify)
    {
        if (evidence == null || string.IsNullOrWhiteSpace(evidence.EvidenceId))
        {
            return false;
        }

        if (_evidenceById.ContainsKey(evidence.EvidenceId))
        {
            return false;
        }

        _evidenceById.Add(evidence.EvidenceId, evidence);

        if (notify)
        {
            EvidenceAdded?.Invoke(evidence);
            Debug.Log($"Evidence acquired: {evidence.DisplayName} ({evidence.EvidenceId})");
        }

        return true;
    }
}
