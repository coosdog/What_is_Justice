using UnityEngine;

public abstract class BasePanelUI : MonoBehaviour
{
    [SerializeField] protected GameObject panelRoot;
    [SerializeField] private bool hideGameplayHudWhileVisible = true;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    protected virtual void Awake()
    {
        EnsurePanelRoot();
    }

    public virtual void Show()
    {
        EnsurePanelRoot();
        if (hideGameplayHudWhileVisible)
        {
            UIManager.Instance?.RequestHudHide(this);
        }

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

        if (hideGameplayHudWhileVisible)
        {
            UIManager.Instance?.ReleaseHudHide(this);
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
