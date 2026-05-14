using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PointerClick2D))]
public sealed class NpcInteractionObject : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private ClueData conversationData;
    [SerializeField] private string npcId;
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private NpcInquiryManager inquiryManager;
    [SerializeField] private InteractionChoiceUI choiceUI;
    [SerializeField] private InvestigationUI investigationUI;

    private PointerClick2D _clickable;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        _clickable = GetComponent<PointerClick2D>();

        if (interactionManager == null)
        {
            interactionManager = FindFirstObjectByType<InteractionManager>();
        }

        if (inquiryManager == null)
        {
            inquiryManager = FindFirstObjectByType<NpcInquiryManager>();
        }

        if (choiceUI == null)
        {
            choiceUI = FindFirstObjectByType<InteractionChoiceUI>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
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
        ResolveReferences();

        if ((investigationUI != null && investigationUI.IsVisible) || (choiceUI != null && choiceUI.IsVisible))
        {
            return;
        }

        if (choiceUI == null)
        {
            Debug.LogWarning($"{name} could not open interaction choices because InteractionChoiceUI was not found.");
            return;
        }

        string title = !string.IsNullOrWhiteSpace(displayName) ? displayName : name;
        choiceUI.Show(title, RunTalk, RunInquiry);
    }

    private void RunTalk()
    {
        if (interactionManager != null && conversationData != null)
        {
            interactionManager.StartInvestigation(conversationData);
        }
    }

    private void RunInquiry()
    {
        if (inquiryManager != null && !string.IsNullOrWhiteSpace(npcId))
        {
            inquiryManager.StartInquiry(npcId);
        }
    }
}
