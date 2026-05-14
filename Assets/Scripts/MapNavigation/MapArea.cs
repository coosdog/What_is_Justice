using UnityEngine;

public sealed class MapArea : MonoBehaviour
{
    [SerializeField] private string areaId;
    [SerializeField] private SpriteRenderer boundsRenderer;
    [SerializeField] private Transform cameraFocusPoint;

    public string AreaId => areaId;
    public SpriteRenderer BoundsRenderer => boundsRenderer;

    private void Awake()
    {
        if (boundsRenderer == null)
        {
            boundsRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public Vector3 GetCameraFocusPosition(float cameraZ)
    {
        if (cameraFocusPoint != null)
        {
            Vector3 focus = cameraFocusPoint.position;
            focus.z = cameraZ;
            return focus;
        }

        if (boundsRenderer != null)
        {
            Bounds bounds = boundsRenderer.bounds;
            return new Vector3(bounds.center.x, bounds.center.y, cameraZ);
        }

        Vector3 position = transform.position;
        position.z = cameraZ;
        return position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(areaId))
        {
            areaId = name;
        }

        if (boundsRenderer == null)
        {
            boundsRenderer = GetComponent<SpriteRenderer>();
        }
    }
#endif
}
