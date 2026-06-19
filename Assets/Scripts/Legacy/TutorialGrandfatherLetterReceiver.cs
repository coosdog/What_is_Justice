using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PointerClick2D))]
public sealed class TutorialGrandfatherLetterReceiver : MonoBehaviour
{
    private const string SpeakerChild = "child_owl";
    private const string NoLetterText = "\uC9C0\uAE08\uC740 \uD560\uC544\uBC84\uC9C0\uC5D0\uAC8C \uAC00\uC838\uAC08 \uB9CC\uD55C \uAC83\uC774 \uC5C6\uB2E4.";

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private TutorialFlowController tutorialFlowController;
    [SerializeField] private string requiredItemId = "tutorial.letter";

    private PointerClick2D _clickable;
    private bool _delivered;

    private void Awake()
    {
        _clickable = GetComponent<PointerClick2D>();

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }

        if (tutorialFlowController == null)
        {
            tutorialFlowController = FindFirstObjectByType<TutorialFlowController>();
        }
    }

    private void OnEnable()
    {
        if (_clickable != null)
        {
            _clickable.Clicked += HandleClicked;
        }
    }

    private void OnDisable()
    {
        if (_clickable != null)
        {
            _clickable.Clicked -= HandleClicked;
        }
    }

    private void HandleClicked()
    {
        if (_delivered || investigationUI != null && investigationUI.IsVisible)
        {
            return;
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (inventoryManager == null || !inventoryManager.HasItem(requiredItemId))
        {
            ShowLine(SpeakerChild, NoLetterText, "child_owl");
            return;
        }

        _delivered = true;
        tutorialFlowController?.DeliverLetterToGrandfather();
    }

    private void ShowLine(string speakerKey, string text, string portraitKey)
    {
        investigationUI?.ShowSequence(new[]
        {
            new DialogueLine(speakerKey, text, portraitKey)
        });
    }
}
