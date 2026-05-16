using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class InvestigationNotebookUI : BasePanelUI
{
    private enum NotebookTab
    {
        Evidence,
        Npc,
        DialogueLog,
        Reflection,
        Assistant
    }

    private const string TmpPrewarmText = "\uC218\uC0AC\uB178\uD2B8 \uB2E8\uC11C NPC \uB300\uD654 \uAE30\uB85D \uC0AC\uC0C9\uD558\uAE30 \uC870\uC218\uC640 \uB300\uD654\uD558\uAE30";

    [Header("Scene References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dispositionText;
    [SerializeField] private TMP_Text leftTitleText;
    [SerializeField] private TMP_Text leftBodyText;
    [SerializeField] private TMP_Text rightTitleText;
    [SerializeField] private TMP_Text rightBodyText;
    [SerializeField] private Button evidenceTabButton;
    [SerializeField] private Button npcTabButton;
    [SerializeField] private Button dialogueLogTabButton;
    [SerializeField] private Button reflectionTabButton;
    [SerializeField] private Button assistantTabButton;
    [SerializeField] private Button closeButton;

    [Header("Data")]
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private NpcProfileRegistry npcProfileRegistry;
    [SerializeField] private AssistantDiscussionManager assistantDiscussionManager;
    [SerializeField] private PlayerDispositionManager dispositionManager;

    [Header("Input")]
    [SerializeField] private bool toggleWithKeyboard = true;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.N;

    private NotebookTab _currentTab = NotebookTab.Evidence;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();

        BindButtons();
        PrewarmTexts();
        Hide();
    }

    private void OnEnable()
    {
        if (evidenceInventory != null)
        {
            evidenceInventory.EvidenceAdded += HandleInventoryChanged;
            evidenceInventory.KeywordAdded += HandleInventoryChanged;
            evidenceInventory.CsvEvidenceAdded += HandleInventoryChanged;
            evidenceInventory.CsvKeywordAdded += HandleInventoryChanged;
        }

        if (dialogueLog != null)
        {
            dialogueLog.Changed += RefreshIfVisible;
        }

        if (dispositionManager != null)
        {
            dispositionManager.DispositionChanged += HandleDispositionChanged;
        }
    }

    private void OnDisable()
    {
        if (evidenceInventory != null)
        {
            evidenceInventory.EvidenceAdded -= HandleInventoryChanged;
            evidenceInventory.KeywordAdded -= HandleInventoryChanged;
            evidenceInventory.CsvEvidenceAdded -= HandleInventoryChanged;
            evidenceInventory.CsvKeywordAdded -= HandleInventoryChanged;
        }

        if (dialogueLog != null)
        {
            dialogueLog.Changed -= RefreshIfVisible;
        }

        if (dispositionManager != null)
        {
            dispositionManager.DispositionChanged -= HandleDispositionChanged;
        }
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
        ResolveReferences();
        base.Show();
        Refresh();
    }

    public override void Hide()
    {
        base.Hide();
    }

    public void ShowEvidenceTab() => SelectTab(NotebookTab.Evidence);
    public void ShowNpcTab() => SelectTab(NotebookTab.Npc);
    public void ShowDialogueLogTab() => SelectTab(NotebookTab.DialogueLog);
    public void ShowReflectionTab() => SelectTab(NotebookTab.Reflection);
    public void ShowAssistantTab() => SelectTab(NotebookTab.Assistant);

    private void SelectTab(NotebookTab tab)
    {
        _currentTab = tab;
        Refresh();
    }

    private void Refresh()
    {
        if (titleText != null)
        {
            titleText.text = "수사노트";
        }

        if (dispositionText != null)
        {
            string disposition = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본";
            dispositionText.text = $"현재 성향: {disposition}    1 기본 / 2 성향 1 / 3 성향 2";
        }

        switch (_currentTab)
        {
            case NotebookTab.Npc:
                RenderNpcTab();
                break;
            case NotebookTab.DialogueLog:
                RenderDialogueLogTab();
                break;
            case NotebookTab.Reflection:
                RenderReflectionTab();
                break;
            case NotebookTab.Assistant:
                RenderAssistantTab();
                break;
            default:
                RenderEvidenceTab();
                break;
        }
    }

    private void RenderEvidenceTab()
    {
        SetTitles("단서 이미지", "모은 단서");
        SetBodies("선택한 단서의 이미지를 이 영역에 배치할 예정입니다.", BuildEvidenceText());
    }

    private void RenderNpcTab()
    {
        SetTitles("NPC 이미지", "NPC 정보");
        StringBuilder builder = new();

        if (npcProfileRegistry != null)
        {
            foreach (NpcProfile profile in npcProfileRegistry.Profiles)
            {
                builder.AppendLine($"[{profile.displayName}]");
                AppendIfNotEmpty(builder, profile.description);
                AppendIfNotEmpty(builder, profile.currentImpression);
                builder.AppendLine();
            }
        }

        if (builder.Length == 0)
        {
            builder.AppendLine("아직 등록된 NPC 정보가 없습니다.");
        }

        SetBodies("선택한 NPC의 초상이나 단서를 이 영역에 배치할 예정입니다.", builder.ToString());
    }

    private void RenderDialogueLogTab()
    {
        SetTitles("대화 장면", "대화 로그");
        StringBuilder builder = new();

        if (dialogueLog != null)
        {
            foreach (DialogueLine line in dialogueLog.Entries)
            {
                string speaker = string.IsNullOrWhiteSpace(line.Speaker) ? "기록" : line.Speaker;
                builder.AppendLine($"{speaker}: {line.Text}");
            }
        }

        if (builder.Length == 0)
        {
            builder.AppendLine("아직 기록된 대화가 없습니다.");
        }

        SetBodies("최근 대화와 연결된 장면 이미지를 배치할 예정입니다.", builder.ToString());
    }

    private void RenderReflectionTab()
    {
        SetTitles("사색하기", "현재 정리");
        string disposition = dispositionManager != null ? dispositionManager.GetDisplayName() : "기본";
        string body = assistantDiscussionManager != null
            ? assistantDiscussionManager.BuildReflectionText()
            : $"{disposition} 성향으로 지금까지 얻은 단서를 정리합니다.";

        SetBodies("플레이어가 혼자 단서를 재배열하는 영역입니다.", body);
    }

    private void RenderAssistantTab()
    {
        SetTitles("조수와 대화하기", "조수의 정리");
        string body = assistantDiscussionManager != null
            ? assistantDiscussionManager.BuildAssistantSummary()
            : "조수가 아직 연결되지 않았습니다.";

        SetBodies("조수 NPC와 함께 추론을 맞춰가는 영역입니다.", body);
    }

    private string BuildEvidenceText()
    {
        if (evidenceInventory == null)
        {
            return "단서 인벤토리를 찾지 못했습니다.";
        }

        StringBuilder builder = new();
        int entryCount = 0;
        builder.AppendLine("[단서]");
        foreach (EvidenceData evidence in evidenceInventory.Evidence)
        {
            builder.AppendLine($"- {evidence.DisplayName}: {evidence.Description}");
            entryCount++;
        }

        foreach (CsvEvidenceRecord evidence in evidenceInventory.CsvEvidence)
        {
            builder.AppendLine($"- {evidence.DisplayName}: {evidence.Description}");
            entryCount++;
        }

        builder.AppendLine();
        builder.AppendLine("[키워드]");
        foreach (KeywordData keyword in evidenceInventory.Keywords)
        {
            builder.AppendLine($"- {keyword.DisplayName}: {keyword.Description}");
            entryCount++;
        }

        foreach (CsvKeywordRecord keyword in evidenceInventory.CsvKeywords)
        {
            builder.AppendLine($"- {keyword.DisplayName}: {keyword.Description}");
            entryCount++;
        }

        if (entryCount == 0)
        {
            builder.AppendLine("아직 획득한 단서가 없습니다.");
        }

        return builder.ToString();
    }

    private void SetTitles(string leftTitle, string rightTitle)
    {
        if (leftTitleText != null)
        {
            leftTitleText.text = leftTitle;
        }

        if (rightTitleText != null)
        {
            rightTitleText.text = rightTitle;
        }
    }

    private void SetBodies(string leftBody, string rightBody)
    {
        if (leftBodyText != null)
        {
            leftBodyText.text = leftBody;
        }

        if (rightBodyText != null)
        {
            rightBodyText.text = rightBody;
        }
    }

    private void ResolveReferences()
    {
        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        if (dialogueLog == null)
        {
            dialogueLog = FindFirstObjectByType<DialogueLog>();
        }

        if (npcProfileRegistry == null)
        {
            npcProfileRegistry = FindFirstObjectByType<NpcProfileRegistry>();
        }

        if (assistantDiscussionManager == null)
        {
            assistantDiscussionManager = FindFirstObjectByType<AssistantDiscussionManager>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    private void BindButtons()
    {
        BindButton(evidenceTabButton, ShowEvidenceTab);
        BindButton(npcTabButton, ShowNpcTab);
        BindButton(dialogueLogTabButton, ShowDialogueLogTab);
        BindButton(reflectionTabButton, ShowReflectionTab);
        BindButton(assistantTabButton, ShowAssistantTab);
        BindButton(closeButton, Hide);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void PrewarmTexts()
    {
        TmpTextPrewarmUtility.Prewarm(titleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(dispositionText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(leftTitleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(leftBodyText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(rightTitleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(rightBodyText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(evidenceTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(npcTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(dialogueLogTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(reflectionTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(assistantTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(closeButton, TmpPrewarmText);
    }

    private void HandleInventoryChanged(EvidenceData _) => RefreshIfVisible();
    private void HandleInventoryChanged(KeywordData _) => RefreshIfVisible();
    private void HandleInventoryChanged(CsvEvidenceRecord _) => RefreshIfVisible();
    private void HandleInventoryChanged(CsvKeywordRecord _) => RefreshIfVisible();
    private void HandleDispositionChanged(PlayerDisposition _) => RefreshIfVisible();

    private void RefreshIfVisible()
    {
        if (IsVisible)
        {
            Refresh();
        }
    }

    private static void AppendIfNotEmpty(StringBuilder builder, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine(value);
        }
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
