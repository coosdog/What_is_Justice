using UnityEngine;

public sealed class AssistantNpcObject : ClickableInquiryObject
{
    [SerializeField] private AssistantCompanionManager assistantCompanionManager;
    [SerializeField] private AssistantDiscussionManager assistantDiscussionManager;

    protected override void Awake()
    {
        base.Awake();
        if (assistantCompanionManager == null)
        {
            assistantCompanionManager = FindFirstObjectByType<AssistantCompanionManager>();
        }

        if (assistantCompanionManager == null)
        {
            assistantCompanionManager = gameObject.AddComponent<AssistantCompanionManager>();
        }

        if (assistantDiscussionManager == null)
        {
            assistantDiscussionManager = FindFirstObjectByType<AssistantDiscussionManager>();
        }
    }

    protected override void OnClicked()
    {
        if (assistantCompanionManager == null)
        {
            assistantCompanionManager = FindFirstObjectByType<AssistantCompanionManager>();
        }

        if (assistantCompanionManager == null)
        {
            assistantCompanionManager = gameObject.AddComponent<AssistantCompanionManager>();
        }

        if (assistantDiscussionManager == null)
        {
            assistantDiscussionManager = FindFirstObjectByType<AssistantDiscussionManager>();
        }

        if (assistantCompanionManager != null)
        {
            if (assistantCompanionManager.StartManualTalk())
            {
                return;
            }
        }

        assistantDiscussionManager?.StartAssistantTalk();
    }
}
