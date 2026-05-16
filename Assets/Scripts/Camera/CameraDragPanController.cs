using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class CameraDragPanController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer boundsRenderer;
    [SerializeField] private InvestigationUI blockingUI;

    [Header("Input")]
    [SerializeField] private bool dragOnlyFromEmptySpace = true;
    [SerializeField] private LayerMask blockingLayers = ~0;

    [Header("Movement")]
    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private Vector2 boundsPadding;

    private bool _isDragging;
    private bool _isDragEnabled = true;
    private Vector3 _dragOriginWorld;

    public Camera TargetCamera => targetCamera;
    public SpriteRenderer BoundsRenderer => boundsRenderer;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (blockingUI == null)
        {
            blockingUI = FindFirstObjectByType<InvestigationUI>();
        }
    }

    private void LateUpdate()
    {
        if (!_isDragEnabled)
        {
            _isDragging = false;
            return;
        }

        if (targetCamera == null || !TryGetPointerState(out Vector2 screenPosition, out bool pressedThisFrame, out bool isPressed))
        {
            _isDragging = false;
            return;
        }

        if (pressedThisFrame)
        {
            _isDragging = CanStartDrag(screenPosition);
            _dragOriginWorld = ScreenToCameraPlaneWorld(screenPosition);
        }

        if (!_isDragging)
        {
            return;
        }

        if (!isPressed)
        {
            _isDragging = false;
            return;
        }

        Vector3 currentWorld = ScreenToCameraPlaneWorld(screenPosition);
        Vector3 delta = (_dragOriginWorld - currentWorld) * dragSpeed;
        Vector3 nextPosition = targetCamera.transform.position + delta;
        targetCamera.transform.position = ClampCameraPosition(nextPosition);
    }

    public void SetDragEnabled(bool enabled)
    {
        _isDragEnabled = enabled;
        if (!enabled)
        {
            _isDragging = false;
        }
    }

    public void SetBoundsRenderer(SpriteRenderer nextBoundsRenderer, bool clampImmediately = true)
    {
        boundsRenderer = nextBoundsRenderer;
        if (clampImmediately)
        {
            ClampCurrentPositionToBounds();
        }
    }

    public void CenterOnBounds()
    {
        if (targetCamera == null || boundsRenderer == null)
        {
            return;
        }

        Bounds bounds = boundsRenderer.bounds;
        Vector3 centeredPosition = new Vector3(bounds.center.x, bounds.center.y, targetCamera.transform.position.z);
        targetCamera.transform.position = ClampCameraPosition(centeredPosition);
    }

    public void ClampCurrentPositionToBounds()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.transform.position = ClampCameraPosition(targetCamera.transform.position);
    }

    public Vector3 GetCenteredPositionFor(SpriteRenderer targetBoundsRenderer)
    {
        if (targetCamera == null || targetBoundsRenderer == null)
        {
            return targetCamera != null ? targetCamera.transform.position : Vector3.zero;
        }

        Bounds bounds = targetBoundsRenderer.bounds;
        return ClampCameraPositionToBounds(
            new Vector3(bounds.center.x, bounds.center.y, targetCamera.transform.position.z),
            targetBoundsRenderer);
    }

    private bool CanStartDrag(Vector2 screenPosition)
    {
        if (blockingUI != null && blockingUI.IsVisible)
        {
            return false;
        }

        if (PointerUiBlocker.IsBlocked(screenPosition))
        {
            return false;
        }

        if (!dragOnlyFromEmptySpace)
        {
            return true;
        }

        Vector2 worldPoint = ScreenToCameraPlaneWorld(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPoint, blockingLayers);
        return hit == null;
    }

    private Vector3 ScreenToCameraPlaneWorld(Vector2 screenPosition)
    {
        Vector3 point = new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z);
        return targetCamera.ScreenToWorldPoint(point);
    }

    private Vector3 ClampCameraPosition(Vector3 position)
    {
        return ClampCameraPositionToBounds(position, boundsRenderer);
    }

    private Vector3 ClampCameraPositionToBounds(Vector3 position, SpriteRenderer rendererToUse)
    {
        if (rendererToUse == null || targetCamera == null || !targetCamera.orthographic)
        {
            return position;
        }

        Bounds bounds = rendererToUse.bounds;
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        float minX = bounds.min.x + halfWidth - boundsPadding.x;
        float maxX = bounds.max.x - halfWidth + boundsPadding.x;
        float minY = bounds.min.y + halfHeight - boundsPadding.y;
        float maxY = bounds.max.y - halfHeight + boundsPadding.y;

        if (minX > maxX)
        {
            position.x = bounds.center.x;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minY > maxY)
        {
            position.y = bounds.center.y;
        }
        else
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        return position;
    }

    private static bool TryGetPointerState(out Vector2 screenPosition, out bool pressedThisFrame, out bool isPressed)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            screenPosition = default;
            pressedThisFrame = false;
            isPressed = false;
            return false;
        }

        screenPosition = mouse.position.ReadValue();
        pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
        isPressed = mouse.leftButton.isPressed;
        return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        pressedThisFrame = Input.GetMouseButtonDown(0);
        isPressed = Input.GetMouseButton(0);
        return true;
#else
        screenPosition = default;
        pressedThisFrame = false;
        isPressed = false;
        return false;
#endif
    }
}
