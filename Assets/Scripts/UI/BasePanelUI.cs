using UnityEngine;

public abstract class BasePanelUI : MonoBehaviour
{
    [SerializeField] protected GameObject panelRoot;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    protected virtual void Awake()
    {
        EnsurePanelRoot();
    }

    public virtual void Show()
    {
        EnsurePanelRoot();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public virtual void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    protected void EnsurePanelRoot()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
    }
}
