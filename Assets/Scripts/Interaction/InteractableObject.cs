using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Scene object that can be investigated by clicking it.
/// It owns a ClueData reference and emits a click event for the manager.
/// </summary>
public sealed class InteractableObject : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ClueData clueData;

    public event Action<InteractableObject> Clicked;

    private Collider2D _collider;
    private Camera _camera;
    private PointerClick2D _clickable;
    private bool _useLegacyClickDetection;

    public ClueData Data => clueData;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _clickable = GetComponent<PointerClick2D>();
        _camera = Camera.main;

        _useLegacyClickDetection = _clickable == null;
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

    private void Update()
    {
        if (!_useLegacyClickDetection)
        {
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera == null || _collider == null || !TryGetClickPosition(out Vector2 screenPosition))
        {
            return;
        }

        if (PointerUiBlocker.IsBlocked(screenPosition))
        {
            return;
        }

        Vector3 mouseWorldPosition = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
        if (_collider.OverlapPoint(mouseWorldPosition))
        {
            HandleClicked();
        }
    }

    private void HandleClicked()
    {
        Clicked?.Invoke(this);
    }

    private static bool TryGetClickPosition(out Vector2 screenPosition)
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
