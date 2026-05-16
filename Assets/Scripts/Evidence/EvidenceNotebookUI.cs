using System.Text;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class EvidenceNotebookUI : BasePanelUI
{
    private const string TmpPrewarmText = "\uAC00\uB098\uB2E4\uB77C\uB9C8\uBC14\uC0AC\uC544\uC790\uCC28\uCE74\uD0C0\uD30C\uD558 \uC99D\uAC70 \uB2E8\uC11C \uD0A4\uC6CC\uB4DC \uC54C\uB9AC\uBC14\uC774";

    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private bool toggleWithKeyboard = true;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.N;

    protected override void Awake()
    {
        base.Awake();

        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        TmpTextPrewarmUtility.Prewarm(bodyText, TmpPrewarmText);
        Hide();
    }

    private void Update()
    {
        if (toggleWithKeyboard && WasTogglePressedThisFrame())
        {
            Toggle();
        }
    }

    public override void Show()
    {
        Refresh();
        base.Show();
    }

    public override void Hide()
    {
        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }
        base.Hide();
    }

    public void Refresh()
    {
        if (bodyText == null)
        {
            return;
        }

        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (evidenceInventory == null)
        {
            bodyText.text = "Evidence inventory not found.";
            return;
        }

        StringBuilder builder = new();
        builder.AppendLine("[Evidence]");
        foreach (EvidenceData evidence in evidenceInventory.Evidence)
        {
            builder.AppendLine($"- {evidence.DisplayName}: {evidence.Description}");
        }

        foreach (CsvEvidenceRecord evidence in evidenceInventory.CsvEvidence)
        {
            builder.AppendLine($"- {evidence.DisplayName}: {evidence.Description}");
        }

        builder.AppendLine();
        builder.AppendLine("[Keywords]");
        foreach (KeywordData keyword in evidenceInventory.Keywords)
        {
            builder.AppendLine($"- {keyword.DisplayName}: {keyword.Description}");
        }

        foreach (CsvKeywordRecord keyword in evidenceInventory.CsvKeywords)
        {
            builder.AppendLine($"- {keyword.DisplayName}: {keyword.Description}");
        }

        bodyText.text = builder.ToString();
    }

    private bool WasTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.nKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(legacyToggleKey);
#else
        return false;
#endif
    }

}
