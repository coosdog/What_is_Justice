using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Scene object that can be investigated by clicking it.
/// It owns a ClueData reference and emits a click event for the manager.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class InteractableObject : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ClueData clueData;

    [Header("Highlight")]
    [Tooltip("SpriteRenderer used for hover highlighting. If empty, this object's SpriteRenderer is used.")]
    [SerializeField] private SpriteRenderer highlightTarget;

    [Tooltip("Color applied while the mouse is hovering over this object.")]
    [SerializeField] private Color hoverColor = Color.yellow;

    [Tooltip("Time used to blend back and forth between normal and hover colors. Use 0 for instant changes.")]
    [SerializeField] private float colorLerpDuration = 0.08f;

    public event Action<InteractableObject> Clicked;

    private Collider2D _collider;
    private Camera _camera;
    private Color _defaultColor;
    private bool _hasHighlight;
    private bool _isHovered;

    public ClueData Data => clueData;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _camera = Camera.main;

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

    private void Update()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera == null || _collider == null || !TryGetPointerState(out Vector2 screenPosition, out bool clickedThisFrame))
        {
            return;
        }

        Vector3 mouseWorldPosition = _camera.ScreenToWorldPoint(screenPosition);
        Vector2 mouseWorldPoint = mouseWorldPosition;
        bool isMouseOver = _collider.OverlapPoint(mouseWorldPoint);

        if (isMouseOver != _isHovered)
        {
            _isHovered = isMouseOver;
            ApplyHighlight(_isHovered);
        }

        if (_isHovered && clickedThisFrame)
        {
            Clicked?.Invoke(this);
        }
    }

    private static bool TryGetPointerState(out Vector2 screenPosition, out bool clickedThisFrame)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            screenPosition = default;
            clickedThisFrame = false;
            return false;
        }

        screenPosition = mouse.position.ReadValue();
        clickedThisFrame = mouse.leftButton.wasPressedThisFrame;
        return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        clickedThisFrame = Input.GetMouseButtonDown(0);
        return true;
#else
        screenPosition = default;
        clickedThisFrame = false;
        return false;
#endif
    }

    private void ApplyHighlight(bool enable)
    {
        if (!_hasHighlight)
        {
            return;
        }

        if (colorLerpDuration <= 0f)
        {
            highlightTarget.color = enable ? hoverColor : _defaultColor;
            return;
        }

        StopAllCoroutines();
        StartCoroutine(LerpColor(enable ? hoverColor : _defaultColor, colorLerpDuration));
    }

    private System.Collections.IEnumerator LerpColor(Color targetColor, float duration)
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
    }
}
