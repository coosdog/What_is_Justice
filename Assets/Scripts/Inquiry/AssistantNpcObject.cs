using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PointerClick2D))]
public sealed class AssistantNpcObject : MonoBehaviour
{
    [SerializeField] private AssistantDiscussionManager assistantDiscussionManager;

    private PointerClick2D _clickable;

    private void Awake()
    {
        _clickable = GetComponent<PointerClick2D>();
        if (assistantDiscussionManager == null)
        {
            assistantDiscussionManager = FindFirstObjectByType<AssistantDiscussionManager>();
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
        if (assistantDiscussionManager == null)
        {
            assistantDiscussionManager = FindFirstObjectByType<AssistantDiscussionManager>();
        }

        assistantDiscussionManager?.StartAssistantTalk();
    }
}
