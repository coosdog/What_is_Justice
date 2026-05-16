using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class DispositionHighlightTarget : MonoBehaviour
{
    [SerializeField] private PlayerDisposition emphasizedDisposition = PlayerDisposition.Basic;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color basicColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color emphasizedColor = new Color(1f, 0.86f, 0.18f, 1f);
    [SerializeField] private bool dimWhenNotMatched = true;

    private PlayerDispositionManager _dispositionManager;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        _dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
    }

    private void OnEnable()
    {
        if (_dispositionManager == null)
        {
            _dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }

        if (_dispositionManager != null)
        {
            _dispositionManager.DispositionChanged += Apply;
            Apply(_dispositionManager.CurrentDisposition);
        }
        else
        {
            Apply(PlayerDisposition.Basic);
        }
    }

    private void OnDisable()
    {
        if (_dispositionManager != null)
        {
            _dispositionManager.DispositionChanged -= Apply;
        }
    }

    private void Apply(PlayerDisposition disposition)
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (disposition == emphasizedDisposition)
        {
            targetRenderer.color = emphasizedColor;
        }
        else
        {
            targetRenderer.color = dimWhenNotMatched ? inactiveColor : basicColor;
        }
    }
}
