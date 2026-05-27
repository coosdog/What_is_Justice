using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PointerHover2D))]
public sealed class SpriteHoverHighlight : MonoBehaviour
{
    [Tooltip("SpriteRenderer used for hover outlining. If empty, this object's SpriteRenderer is used.")]
    [SerializeField] private SpriteRenderer highlightTarget;

    [Tooltip("Outline color applied while the mouse is hovering over this object.")]
    [SerializeField] private Color hoverColor = Color.yellow;

    [Tooltip("Time used to fade the outline in and out. Use 0 for instant changes.")]
    [SerializeField] private float colorLerpDuration = 0.08f;

    [Tooltip("Outline thickness in local sprite units.")]
    [SerializeField] private float outlineThickness = 0.04f;

    private PointerHover2D _hover;
    private bool _hasHighlight;
    private bool _isHovered;
    private Coroutine _fadeRoutine;
    private SpriteRenderer[] _outlineRenderers;

    private static readonly Vector3[] OutlineOffsets =
    {
        new Vector3(-1f, 0f, 0f),
        new Vector3(1f, 0f, 0f),
        new Vector3(0f, -1f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(-1f, -1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3(1f, -1f, 0f),
        new Vector3(1f, 1f, 0f),
    };

    private void Awake()
    {
        _hover = GetComponent<PointerHover2D>();
        if (highlightTarget == null)
        {
            highlightTarget = GetComponent<SpriteRenderer>();
        }

        _hasHighlight = highlightTarget != null;
        if (_hasHighlight)
        {
            BuildOutline();
            SetOutlineAlpha(0f);
        }
    }

    private void OnEnable()
    {
        if (_hover != null)
        {
            _hover.HoverChanged += ApplyHighlight;
            ApplyHighlight(_hover.IsHovered);
        }
    }

    private void OnDisable()
    {
        if (_hover != null)
        {
            _hover.HoverChanged -= ApplyHighlight;
        }

        ApplyHighlight(false);
    }

    private void LateUpdate()
    {
        if (_hasHighlight && _outlineRenderers != null)
        {
            SyncOutlineRenderers();
        }
    }

    private void ApplyHighlight(bool enable)
    {
        _isHovered = enable;

        if (!_hasHighlight)
        {
            return;
        }

        if (colorLerpDuration <= 0f || !isActiveAndEnabled)
        {
            SetOutlineAlpha(enable ? hoverColor.a : 0f);
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeOutline(enable ? hoverColor.a : 0f, colorLerpDuration));
    }

    private void BuildOutline()
    {
        GameObject root = new GameObject($"{nameof(SpriteHoverHighlight)}Outline");
        root.transform.SetParent(highlightTarget.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        _outlineRenderers = new SpriteRenderer[OutlineOffsets.Length];
        for (int i = 0; i < _outlineRenderers.Length; i++)
        {
            GameObject outlineObject = new GameObject($"Outline_{i}");
            outlineObject.transform.SetParent(root.transform, false);
            outlineObject.transform.localPosition = OutlineOffsets[i] * outlineThickness;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            SpriteRenderer outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
            _outlineRenderers[i] = outlineRenderer;
        }

        SyncOutlineRenderers();
    }

    private void SyncOutlineRenderers()
    {
        for (int i = 0; i < _outlineRenderers.Length; i++)
        {
            SpriteRenderer outlineRenderer = _outlineRenderers[i];
            if (outlineRenderer == null)
            {
                continue;
            }

            outlineRenderer.sprite = highlightTarget.sprite;
            outlineRenderer.drawMode = highlightTarget.drawMode;
            outlineRenderer.size = highlightTarget.size;
            outlineRenderer.flipX = highlightTarget.flipX;
            outlineRenderer.flipY = highlightTarget.flipY;
            outlineRenderer.sharedMaterial = highlightTarget.sharedMaterial;
            outlineRenderer.sortingLayerID = highlightTarget.sortingLayerID;
            outlineRenderer.sortingOrder = highlightTarget.sortingOrder - 1;
            outlineRenderer.maskInteraction = highlightTarget.maskInteraction;
            outlineRenderer.transform.localPosition = OutlineOffsets[i] * outlineThickness;
        }
    }

    private IEnumerator FadeOutline(float targetAlpha, float duration)
    {
        float startAlpha = GetOutlineAlpha();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetOutlineAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetOutlineAlpha(targetAlpha);
        _fadeRoutine = null;
    }

    private float GetOutlineAlpha()
    {
        if (_outlineRenderers == null || _outlineRenderers.Length == 0 || _outlineRenderers[0] == null)
        {
            return 0f;
        }

        return _outlineRenderers[0].color.a;
    }

    private void SetOutlineAlpha(float alpha)
    {
        if (_outlineRenderers == null)
        {
            return;
        }

        for (int i = 0; i < _outlineRenderers.Length; i++)
        {
            SpriteRenderer outlineRenderer = _outlineRenderers[i];
            if (outlineRenderer == null)
            {
                continue;
            }

            Color color = hoverColor;
            color.a = alpha;
            outlineRenderer.color = color;
            outlineRenderer.enabled = alpha > 0f || _isHovered;
        }
    }
}
