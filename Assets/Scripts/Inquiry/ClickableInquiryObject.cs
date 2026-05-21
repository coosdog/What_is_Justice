using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PointerClick2D))]
public abstract class ClickableInquiryObject : MonoBehaviour
{
    private PointerClick2D _clickable;

    protected virtual void Awake()
    {
        _clickable = GetComponent<PointerClick2D>();
    }

    protected virtual void OnEnable()
    {
        if (_clickable != null)
        {
            _clickable.Clicked += HandleClicked;
        }
    }

    protected virtual void OnDisable()
    {
        if (_clickable != null)
        {
            _clickable.Clicked -= HandleClicked;
        }
    }

    private void HandleClicked()
    {
        OnClicked();
    }

    protected abstract void OnClicked();
}
