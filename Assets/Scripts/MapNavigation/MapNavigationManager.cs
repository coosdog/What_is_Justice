using System.Collections;
using UnityEngine;

public sealed class MapNavigationManager : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraDragPanController panController;
    [SerializeField] private MapArea startingArea;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _moveRoutine;
    private MapArea _currentArea;

    public MapArea CurrentArea => _currentArea;
    public bool IsMoving => _moveRoutine != null;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (panController == null && targetCamera != null)
        {
            panController = targetCamera.GetComponent<CameraDragPanController>();
        }
    }

    private void Start()
    {
        if (startingArea != null)
        {
            SnapTo(startingArea);
        }
    }

    public void NavigateTo(MapArea targetArea)
    {
        if (targetArea == null || targetCamera == null)
        {
            return;
        }

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }

        _moveRoutine = StartCoroutine(MoveToRoutine(targetArea));
    }

    public void SnapTo(MapArea targetArea)
    {
        if (targetArea == null || targetCamera == null)
        {
            return;
        }

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        _currentArea = targetArea;
        Vector3 focusPosition = targetArea.GetCameraFocusPosition(targetCamera.transform.position.z);
        targetCamera.transform.position = focusPosition;

        if (panController != null)
        {
            panController.SetBoundsRenderer(targetArea.BoundsRenderer, false);
            panController.CenterOnBounds();
            panController.SetDragEnabled(true);
        }
    }

    private IEnumerator MoveToRoutine(MapArea targetArea)
    {
        _currentArea = targetArea;

        if (panController != null)
        {
            panController.SetDragEnabled(false);
        }

        Vector3 startPosition = targetCamera.transform.position;
        Vector3 endPosition = targetArea.GetCameraFocusPosition(startPosition.z);

        if (panController != null && targetArea.BoundsRenderer != null)
        {
            endPosition = panController.GetCenteredPositionFor(targetArea.BoundsRenderer);
        }

        if (moveDuration <= 0f)
        {
            targetCamera.transform.position = endPosition;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;
                targetCamera.transform.position = Vector3.Lerp(startPosition, endPosition, curvedT);
                yield return null;
            }

            targetCamera.transform.position = endPosition;
        }

        if (panController != null)
        {
            panController.SetBoundsRenderer(targetArea.BoundsRenderer, false);
            panController.CenterOnBounds();
            panController.SetDragEnabled(true);
        }

        _moveRoutine = null;
    }
}
