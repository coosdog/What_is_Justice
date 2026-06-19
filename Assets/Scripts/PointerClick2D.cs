using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PointerClick2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool ignoreWhenPointerIsOverUI = true;

    public event Action Clicked;

    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || _collider == null || !WasPressedThisFrame(out Vector2 screenPosition))
        {
            return;
        }

        if (ignoreWhenPointerIsOverUI && PointerUiBlocker.IsBlocked(screenPosition))
        {
            return;
        }

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z));
        if (_collider.OverlapPoint(worldPosition))
        {
            Clicked?.Invoke();
        }
    }

    private static bool WasPressedThisFrame(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = default;
            return false;
        }

        screenPosition = mouse.position.ReadValue();
        return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (!Input.GetMouseButtonDown(0))
        {
            screenPosition = default;
            return false;
        }

        screenPosition = Input.mousePosition;
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }
}

