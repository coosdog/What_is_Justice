using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PointerHover2D))]
public sealed class SpriteHoverHighlight : MonoBehaviour
{
    [Tooltip("SpriteRenderer used for hover highlighting. If empty, this object's SpriteRenderer is used.")]
    [SerializeField] private SpriteRenderer highlightTarget;

    [Tooltip("Color applied while the mouse is hovering over this object.")]
    [SerializeField] private Color hoverColor = Color.yellow;

    [Tooltip("Time used to blend back and forth between normal and hover colors. Use 0 for instant changes.")]
    [SerializeField] private float colorLerpDuration = 0.08f;

    private PointerHover2D _hover;
    private Color _defaultColor;
    private bool _hasHighlight;
    private bool _isHovered;
    private Coroutine _colorRoutine;

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
            _defaultColor = highlightTarget.color;
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

    private void ApplyHighlight(bool enable)
    {
        _isHovered = enable;

        if (!_hasHighlight)
        {
            return;
        }

        Color targetColor = enable ? hoverColor : _defaultColor;
        if (colorLerpDuration <= 0f || !isActiveAndEnabled)
        {
            highlightTarget.color = targetColor;
            return;
        }

        if (_colorRoutine != null)
        {
            StopCoroutine(_colorRoutine);
        }

        _colorRoutine = StartCoroutine(LerpColor(targetColor, colorLerpDuration));
    }

    private IEnumerator LerpColor(Color targetColor, float duration)
    {
        Color start = highlightTarget.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            highlightTarget.color = Color.Lerp(start, targetColor, t);
            yield return null;
        }

        highlightTarget.color = targetColor;

        if (_isHovered && targetColor != hoverColor)
        {
            highlightTarget.color = hoverColor;
        }
        else if (!_isHovered && targetColor != _defaultColor)
        {
            highlightTarget.color = _defaultColor;
        }

        _colorRoutine = null;
    }
}
