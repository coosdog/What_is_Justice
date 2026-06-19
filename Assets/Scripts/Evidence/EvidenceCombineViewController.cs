using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(RectTransform))]
public sealed class EvidenceCombineViewController : MonoBehaviour
{
    [SerializeField] private InvestigationBoardManager investigationBoardManager;
    [SerializeField] private RectTransform lineLayer;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Color pendingLineColor = new(1f, 0.88f, 0.35f, 0.9f);
    [SerializeField] private Color linkedLineColor = new(0.55f, 0.9f, 1f, 0.9f);
    [SerializeField] private float lineThickness = 6f;

    private readonly List<LinkedLine> _linkedLines = new();
    private EvidenceCombineNodeButton _pendingNode;
    private RectTransform _pendingLine;
    private Canvas _canvas;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (_pendingNode != null && _pendingLine != null)
        {
            UpdatePendingLine();
        }

        for (int i = _linkedLines.Count - 1; i >= 0; i--)
        {
            if (!_linkedLines[i].IsValid)
            {
                Destroy(_linkedLines[i].Line.gameObject);
                _linkedLines.RemoveAt(i);
                continue;
            }

            SetLineBetween(_linkedLines[i].Line, _linkedLines[i].Start.RectTransform, _linkedLines[i].End.RectTransform);
        }
    }

    public void HandleNodeClicked(EvidenceCombineNodeButton node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
        {
            SetResult("연결할 노드 ID가 비어 있습니다.");
            return;
        }

        ResolveReferences();

        if (_pendingNode == null)
        {
            BeginPending(node);
            return;
        }

        if (_pendingNode == node)
        {
            CancelPending();
            return;
        }

        if (_pendingNode.NodeType == node.NodeType)
        {
            BeginPending(node);
            SetResult("키워드와 단서를 서로 연결해야 합니다.");
            return;
        }

        LinkNodes(_pendingNode, node);
        CancelPending();
    }

    public void ClearVisualLinks()
    {
        CancelPending();

        foreach (LinkedLine linkedLine in _linkedLines)
        {
            if (linkedLine.Line != null)
            {
                Destroy(linkedLine.Line.gameObject);
            }
        }

        _linkedLines.Clear();
        SetResult(string.Empty);
    }

    private void BeginPending(EvidenceCombineNodeButton node)
    {
        _pendingNode = node;
        EnsurePendingLine();
        SetResult($"{node.NodeId} 선택됨");
    }

    private void CancelPending()
    {
        _pendingNode = null;
        if (_pendingLine != null)
        {
            Destroy(_pendingLine.gameObject);
            _pendingLine = null;
        }
    }

    private void LinkNodes(EvidenceCombineNodeButton first, EvidenceCombineNodeButton second)
    {
        if (investigationBoardManager == null)
        {
            SetResult("InvestigationBoardManager를 찾을 수 없습니다.");
            return;
        }

        LinkResult result = investigationBoardManager.TryConnect(first.NodeId, second.NodeId);
        SetResult(result.Message);

        if (!result.Success || HasVisualLink(first, second))
        {
            return;
        }

        RectTransform line = CreateLine("LinkedLine", linkedLineColor);
        _linkedLines.Add(new LinkedLine(first, second, line));
        SetLineBetween(line, first.RectTransform, second.RectTransform);
    }

    private bool HasVisualLink(EvidenceCombineNodeButton first, EvidenceCombineNodeButton second)
    {
        foreach (LinkedLine linkedLine in _linkedLines)
        {
            if (linkedLine.Matches(first, second))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdatePendingLine()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineLayer,
            GetPointerScreenPosition(),
            GetUiCamera(),
            out Vector2 pointerPosition);

        SetLineBetween(_pendingLine, GetLocalCenter(_pendingNode.RectTransform), pointerPosition);
    }

    private static Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private void SetLineBetween(RectTransform line, RectTransform start, RectTransform end)
    {
        SetLineBetween(line, GetLocalCenter(start), GetLocalCenter(end));
    }

    private void SetLineBetween(RectTransform line, Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        line.anchoredPosition = start + delta * 0.5f;
        line.sizeDelta = new Vector2(delta.magnitude, lineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Vector2 GetLocalCenter(RectTransform target)
    {
        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), worldCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineLayer, screenPoint, GetUiCamera(), out Vector2 localPoint);
        return localPoint;
    }

    private void EnsurePendingLine()
    {
        if (_pendingLine == null)
        {
            _pendingLine = CreateLine("PendingLine", pendingLineColor);
        }
    }

    private RectTransform CreateLine(string objectName, Color color)
    {
        ResolveReferences();

        GameObject lineObject = new(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = (RectTransform)lineObject.transform;
        rectTransform.SetParent(lineLayer, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(0f, lineThickness);

        Image image = lineObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rectTransform;
    }

    private void ResolveReferences()
    {
        investigationBoardManager ??= FindFirstObjectByType<InvestigationBoardManager>();
        _canvas ??= GetComponentInParent<Canvas>();

        if (lineLayer == null)
        {
            Transform existingLayer = transform.Find("ConnectionLineLayer");
            if (existingLayer != null)
            {
                lineLayer = existingLayer as RectTransform;
                EnsureIgnoredByLayout(lineLayer);
            }
            else
            {
                lineLayer = CreateLineLayer();
            }
        }
    }

    private RectTransform CreateLineLayer()
    {
        GameObject layerObject = new("ConnectionLineLayer", typeof(RectTransform), typeof(LayoutElement));
        RectTransform rectTransform = (RectTransform)layerObject.transform;
        rectTransform.SetParent(transform, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        LayoutElement layoutElement = layerObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        rectTransform.SetAsLastSibling();
        return rectTransform;
    }

    private static void EnsureIgnoredByLayout(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
    }

    private Camera GetUiCamera()
    {
        if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _canvas.worldCamera;
    }

    private void SetResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message ?? string.Empty;
        }
    }

    private readonly struct LinkedLine
    {
        public LinkedLine(EvidenceCombineNodeButton start, EvidenceCombineNodeButton end, RectTransform line)
        {
            Start = start;
            End = end;
            Line = line;
        }

        public EvidenceCombineNodeButton Start { get; }
        public EvidenceCombineNodeButton End { get; }
        public RectTransform Line { get; }
        public bool IsValid => Start != null && End != null && Line != null && Start.gameObject.activeInHierarchy && End.gameObject.activeInHierarchy;

        public bool Matches(EvidenceCombineNodeButton first, EvidenceCombineNodeButton second)
        {
            return Start == first && End == second || Start == second && End == first;
        }
    }
}
