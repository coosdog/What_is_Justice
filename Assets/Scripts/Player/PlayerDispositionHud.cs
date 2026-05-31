using TMPro;
using UnityEngine;

public sealed class PlayerDispositionHud : MonoBehaviour
{
    private const string TmpPrewarmText = "관찰모드 기본 관찰 예리한 시야 미세한 청각 침묵의 응시";

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
            Debug.LogWarning("PlayerDispositionHud has no label text assigned.");
            return;
        }

        string disposition = dispositionManager != null ? dispositionManager.DisplayName : "기본 관찰";
        labelText.text = $"관찰모드: {disposition}  [1 기본/2 시야/3 청각/4 응시]";
    }
}
