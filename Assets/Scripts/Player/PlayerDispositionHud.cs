using TMPro;
using UnityEngine;

public sealed class PlayerDispositionHud : MonoBehaviour
{
    private const string TmpPrewarmText = "\uD604\uC7AC \uC131\uD5A5 \uAE30\uBCF8 \uC131\uD5A5 1 \uC131\uD5A5 2";

    [SerializeField] private PlayerDispositionManager dispositionManager;
    [SerializeField] private TMP_Text labelText;

    private void Awake()
    {
        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }

        TmpTextPrewarmUtility.Prewarm(labelText, TmpPrewarmText);
        Refresh();
    }

    private void OnEnable()
    {
        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }

        if (dispositionManager != null)
        {
            dispositionManager.DispositionChanged += HandleDispositionChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (dispositionManager != null)
        {
            dispositionManager.DispositionChanged -= HandleDispositionChanged;
        }
    }

    private void HandleDispositionChanged(PlayerDisposition _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (labelText == null)
        {
            return;
        }

        string disposition = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본";
        labelText.text = $"성향: {disposition}  [1/2/3]";
    }

}
