using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class InteractionManager : MonoBehaviour
{
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private BackgroundTransitionManager backgroundTransitionManager;
    [SerializeField] private InteractableObject[] interactables;

    public event Action<ClueData, bool, bool> InvestigationCompleted;

    private readonly HashSet<string> _investigatedClueIds = new();
    private ClueData _activeClueData;
    private bool _activeWasFirstInvestigation;
    private int _lastShownFrame = -1;

    private void Awake()
    {
        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (backgroundTransitionManager == null)
        {
            backgroundTransitionManager = FindFirstObjectByType<BackgroundTransitionManager>();
        }

        if (interactables == null || interactables.Length == 0)
        {
            interactables = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        }

        BindInteractables(true);
    }

    private void Update()
    {
        if (investigationUI == null || !investigationUI.IsVisible)
        {
            return;
        }

        if (Time.frameCount == _lastShownFrame || !WasAdvancePressedThisFrame())
        {
            return;
        }

        if (investigationUI.HasNextLine)
        {
            investigationUI.ShowNextLine();
        }
        else
        {
            investigationUI.Hide();
            CompleteActiveInteraction();
        }
    }

    private void OnDestroy()
    {
        BindInteractables(false);
    }

    private void BindInteractables(bool bind)
    {
        if (interactables == null)
        {
            return;
        }

        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null)
            {
                continue;
            }

            if (bind)
            {
                interactable.Clicked += HandleInteractableClicked;
            }
            else
            {
                interactable.Clicked -= HandleInteractableClicked;
            }
        }
    }

    private void HandleInteractableClicked(InteractableObject interactable)
    {
        if (investigationUI != null && investigationUI.IsVisible)
        {
            return;
        }

        ClueData data = interactable != null ? interactable.Data : null;
        if (data == null)
        {
            return;
        }

        bool isFirstInvestigation = _investigatedClueIds.Add(data.ClueId);
        List<DialogueLine> lines = isFirstInvestigation
            ? ResolveDialogueLines(data.EnumerateFirstInvestigationDialogueIds(), data.DisplayName, data.FirstInvestigationText)
            : ResolveDialogueLines(data.EnumerateAlreadyInvestigatedDialogueIds(), data.DisplayName, data.AlreadyInvestigatedText);

        _activeClueData = data;
        _activeWasFirstInvestigation = isFirstInvestigation;

        if (investigationUI != null)
        {
            investigationUI.ShowSequence(lines);
            _lastShownFrame = Time.frameCount;

            if (!investigationUI.IsVisible)
            {
                CompleteActiveInteraction();
            }
        }
        else
        {
            CompleteActiveInteraction();
        }
    }

    private void CompleteActiveInteraction()
    {
        if (_activeClueData == null)
        {
            return;
        }

        ClueData completedData = _activeClueData;
        bool wasFirstInvestigation = _activeWasFirstInvestigation;
        bool shouldRunOutcomes = wasFirstInvestigation || !completedData.RunOutcomesOnlyOnFirstInvestigation;
        bool grantedEvidence = false;

        _activeClueData = null;
        _activeWasFirstInvestigation = false;

        if (shouldRunOutcomes)
        {
            if (evidenceInventory != null && completedData.RewardEvidence != null)
            {
                grantedEvidence = evidenceInventory.AddEvidence(completedData.RewardEvidence);
            }

            if (backgroundTransitionManager != null && completedData.NextBackground != null)
            {
                backgroundTransitionManager.TransitionTo(completedData.NextBackground);
            }
        }

        InvestigationCompleted?.Invoke(completedData, wasFirstInvestigation, grantedEvidence);
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

    private static bool WasAdvancePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.escapeKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public IReadOnlyCollection<string> GetInvestigatedIds() => _investigatedClueIds;

    public void RestoreInvestigatedIds(IEnumerable<string> investigatedIds)
    {
        _investigatedClueIds.Clear();
        if (investigatedIds == null)
        {
            return;
        }

        foreach (string id in investigatedIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _investigatedClueIds.Add(id);
            }
        }
    }
}
