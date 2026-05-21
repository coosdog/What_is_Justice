using UnityEngine;

public sealed class NpcInteractionObject : ClickableInquiryObject
{
    [SerializeField] private string displayName;
    [SerializeField] private ClueData conversationData;
    [SerializeField] private string npcId;
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private NpcInquiryManager inquiryManager;
    [SerializeField] private InteractionChoiceUI choiceUI;
    [SerializeField] private InvestigationUI investigationUI;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
    }

    private void ResolveReferences()
    {
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

    protected override void OnClicked()
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
