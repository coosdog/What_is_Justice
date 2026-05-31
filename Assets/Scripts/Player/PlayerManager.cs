using System;
using UnityEngine;

public sealed class PlayerManager : PersistentSingleton<PlayerManager>
{
    private const string DefaultPlayerDisplayName = "\uC62C\uBE7C\uBBF8 \uD0D0\uC815";

    [SerializeField] private string playerDisplayName = DefaultPlayerDisplayName;
    [SerializeField] private PlayerDispositionManager dispositionManager;

    public event Action<PlayerDisposition> DispositionChanged;

    public string PlayerDisplayName
    {
        get => string.IsNullOrWhiteSpace(playerDisplayName) ? DefaultPlayerDisplayName : playerDisplayName;
        set => playerDisplayName = string.IsNullOrWhiteSpace(value) ? DefaultPlayerDisplayName : value.Trim();
    }

    public PlayerDisposition CurrentDisposition
    {
        get
        {
            ResolveReferences();
            return dispositionManager != null ? dispositionManager.CurrentDisposition : PlayerDisposition.Basic;
        }
        set
        {
            ResolveReferences();
            if (dispositionManager != null)
            {
                dispositionManager.CurrentDisposition = value;
            }
        }
    }

    public string DispositionDisplayName
    {
        get
        {
            ResolveReferences();
            return dispositionManager != null ? dispositionManager.DisplayName : PlayerDispositionManager.GetDisplayName(CurrentDisposition);
        }
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

    private void OnEnable()
    {
        ResolveReferences();
        if (dispositionManager != null)
        {
            dispositionManager.DispositionChanged += HandleDispositionChanged;
        }
    }

    private void OnDisable()
    {
        if (dispositionManager != null)
        {
            dispositionManager.DispositionChanged -= HandleDispositionChanged;
        }
    }

    private void ResolveReferences()
    {
        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    private void HandleDispositionChanged(PlayerDisposition disposition)
    {
        DispositionChanged?.Invoke(disposition);
    }
}
