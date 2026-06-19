using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class EvidenceCombineNodeButton : MonoBehaviour
{
    [SerializeField] private string nodeId;
    [SerializeField] private InvestigationNodeType nodeType = InvestigationNodeType.Keyword;
    [SerializeField] private Button button;

    private EvidenceCombineViewController _controller;

    public string NodeId => nodeId;
    public InvestigationNodeType NodeType => nodeType;
    public RectTransform RectTransform => (RectTransform)transform;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void ResolveReferences()
    {
        button ??= GetComponent<Button>();
        _controller ??= GetComponentInParent<EvidenceCombineViewController>(true);
    }

    private void HandleClick()
    {
        _controller ??= GetComponentInParent<EvidenceCombineViewController>(true);
        _controller?.HandleNodeClicked(this);
    }
}
