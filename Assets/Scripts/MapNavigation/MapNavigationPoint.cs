using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PointerClick2D))]
public sealed class MapNavigationPoint : MonoBehaviour
{
    [SerializeField] private MapArea targetArea;
    [SerializeField] private MapNavigationManager navigationManager;

    public MapArea TargetArea => targetArea;

    private PointerClick2D _clickable;

    private void Awake()
    {
        _clickable = GetComponent<PointerClick2D>();
        if (navigationManager == null)
        {
            navigationManager = FindFirstObjectByType<MapNavigationManager>();
        }
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

    private void HandleClicked()
    {
        if (navigationManager == null || targetArea == null)
        {
            return;
        }

        navigationManager.NavigateTo(targetArea);
    }
}

