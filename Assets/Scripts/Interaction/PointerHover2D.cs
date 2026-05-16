using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PointerHover2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool ignoreWhenPointerIsOverUI = true;

    public event Action HoverEntered;
    public event Action HoverExited;
    public event Action<bool> HoverChanged;

    private Collider2D _collider;
    private bool _isHovered;

    public bool IsHovered => _isHovered;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        SetHovered(false);
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || _collider == null || !TryGetPointerPosition(out Vector2 screenPosition))
        {
            SetHovered(false);
            return;
        }

        if (ignoreWhenPointerIsOverUI && PointerUiBlocker.IsBlocked(screenPosition))
        {
            SetHovered(false);
            return;
        }

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z));
        SetHovered(_collider.OverlapPoint(worldPosition));
    }

    private void SetHovered(bool hovered)
    {
        if (_isHovered == hovered)
        {
            return;
        }

        _isHovered = hovered;
        HoverChanged?.Invoke(_isHovered);

        if (_isHovered)
        {
            HoverEntered?.Invoke();
        }
        else
        {
            HoverExited?.Invoke();
        }
    }

    private static bool TryGetPointerPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            screenPosition = default;
            return false;
        }

        screenPosition = mouse.position.ReadValue();
        return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }
}
