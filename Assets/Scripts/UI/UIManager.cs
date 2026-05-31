using UnityEngine;

public sealed class UIManager : PersistentSingleton<UIManager>
{
    [SerializeField] private AcquisitionNotificationUI acquisitionNotificationUI;

    public bool IsHudHidden => GameplayHudVisibilityController.IsHudHidden;

    public void RequestHudHide(Object requester)
    {
        if (requester == null)
        {
            return;
        }

        GameplayHudVisibilityController.RequestHide(requester);
    }

    public void ReleaseHudHide(Object requester)
    {
        if (requester == null)
        {
            return;
        }

        GameplayHudVisibilityController.ReleaseHide(requester);
    }

    public void RegisterHudObject(GameObject hudObject)
    {
        GameplayHudVisibilityController.Register(hudObject);
    }

    public void UnregisterHudObject(GameObject hudObject)
    {
        GameplayHudVisibilityController.Unregister(hudObject);
    }

    public void ShowNotification(string title, string body)
    {
        ResolveReferences();
        acquisitionNotificationUI?.ShowNotification(title, body);
    }

    protected override void Awake()
    {
        base.Awake();
        if (!IsActiveSingleton)
        {
            return;
        }

        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (acquisitionNotificationUI == null)
        {
            acquisitionNotificationUI = FindFirstObjectByType<AcquisitionNotificationUI>();
        }
    }
}
