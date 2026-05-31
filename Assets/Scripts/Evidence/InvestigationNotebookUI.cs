using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        Assistant,
        MagicCircle
    }

    private const string TmpPrewarmText = "수사노트 단서 NPC 대화 기록 단서연결 가설 카드 사색에 잠기기 조수의 정리 마법진 추리 중심그림 보조그림 작은문양 기본 관찰 예리한 시야 미세한 청각 침묵의 응시";
    private const int BoardVisibleNodeCount = 16;

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
    [SerializeField] private Button magicCircleTabButton;
    [SerializeField] private Button closeButton;

    [Header("Data")]
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private NpcProfileRegistry npcProfileRegistry;
    [SerializeField] private AssistantDiscussionManager assistantDiscussionManager;
    [SerializeField] private PlayerReflectionManager playerReflectionManager;
    [SerializeField] private InvestigationBoardManager investigationBoardManager;
    [SerializeField] private MagicCircleInferenceManager magicCircleInferenceManager;
    [SerializeField] private PlayerDispositionManager dispositionManager;

    [Header("Input")]
    [SerializeField] private bool toggleWithKeyboard = true;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.N;
    [SerializeField] private bool suppressUiNavigationWhileConnectingClues = true;

    [Header("Overlay")]
    [SerializeField] private GameObject[] hideWhileOpen;

    private NotebookTab _currentTab = NotebookTab.Evidence;
    private readonly Dictionary<GameObject, bool> _hiddenObjectStates = new();
    private int _firstBoardNodeIndex;
    private int _secondBoardNodeIndex = 1;
    private int _boardScrollIndex;
    private string _lastBoardResult = "연결할 단서 두 개를 고르자.";
    private int _magicMinorCursorIndex;
    private MagicCircleInferenceResult _lastMagicCircleResult = MagicCircleInferenceResult.Failed("마법진 조각을 고른 뒤 판정해보자.");
    private EventSystem _eventSystem;
    private GameObject _previousSelectedObject;
    private bool _previousSendNavigationEvents;
    private bool _isSuppressingUiNavigation;
    private MagicCirclePartType _activeMagicPartType = MagicCirclePartType.MainImage;
    private GameObject _magicLeftRoot;
    private GameObject _magicRightRoot;
    private RectTransform _magicPartContent;
    private TMP_Text _magicMainSelectorText;
    private TMP_Text _magicSupportSelectorText;
    private TMP_Text _magicMinorSelectorText;
    private TMP_Text _magicPreviewText;
    private TMP_Text _magicResultText;

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

        if (investigationBoardManager != null)
        {
            investigationBoardManager.Changed += RefreshIfVisible;
        }
    }

    private void OnDisable()
    {
        RestoreUiNavigation();

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

        if (investigationBoardManager != null)
        {
            investigationBoardManager.Changed -= RefreshIfVisible;
        }

        RestoreHiddenObjects();
    }

    private void Update()
    {
        if (toggleWithKeyboard && WasTogglePressedThisFrame())
        {
            Toggle();
        }

        if (IsVisible && _currentTab == NotebookTab.Reflection)
        {
            HandleBoardInput();
        }

        if (IsVisible && _currentTab == NotebookTab.MagicCircle)
        {
            HandleMagicCircleInput();
        }

        if (IsVisible && WasMagicCircleTabPressedThisFrame())
        {
            SelectTab(NotebookTab.MagicCircle);
        }
    }

    public override void Show()
    {
        ResolveReferences();
        HideOverlappedObjects();
        base.Show();
        Refresh();
        UpdateUiNavigationSuppression();
    }

    public override void Hide()
    {
        RestoreUiNavigation();
        base.Hide();
        RestoreHiddenObjects();
    }

    public void ShowEvidenceTab() => SelectTab(NotebookTab.Evidence);
    public void ShowNpcTab() => SelectTab(NotebookTab.Npc);
    public void ShowDialogueLogTab() => SelectTab(NotebookTab.DialogueLog);
    public void ShowReflectionTab() => SelectTab(NotebookTab.Reflection);
    public void ShowAssistantTab() => SelectTab(NotebookTab.Assistant);
    public void ShowMagicCircleTab() => SelectTab(NotebookTab.MagicCircle);

    private void SelectTab(NotebookTab tab)
    {
        _currentTab = tab;
        Refresh();
        UpdateUiNavigationSuppression();
    }

    private void Refresh()
    {
        if (_currentTab != NotebookTab.MagicCircle)
        {
            SetMagicCircleVisualVisible(false);
            SetTextBodiesVisible(true);
        }

        if (titleText != null)
        {
            titleText.text = "수사노트";
        }

        if (dispositionText != null)
        {
            string disposition = dispositionManager != null ? dispositionManager.DisplayName : "기본";
            dispositionText.text = $"관찰모드: {disposition}    1 기본 관찰 / 2 예리한 시야 / 3 미세한 청각 / 4 침묵의 응시";
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
            case NotebookTab.MagicCircle:
                RenderMagicCircleTab();
                break;
            default:
                RenderEvidenceTab();
                break;
        }
    }

    private void RenderEvidenceTab()
    {
        SetTitles("단서 이미지", "모은 단서");
        SetBodies("선택한 단서의 이미지가 표시될 영역입니다.", BuildEvidenceText());
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

        SetBodies("선택한 NPC의 초상이나 단서를 표시할 영역입니다.", builder.ToString());
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

        SetBodies("최근 대화와 연결된 장면 이미지가 표시될 영역입니다.", builder.ToString());
    }

    private void RenderReflectionTab()
    {
        SetTitles("단서 연결", "가설 카드");
        SetBodies(BuildConnectionBoardText(), BuildHypothesisText());
    }

    private void RenderAssistantTab()
    {
        SetTitles("사색에 잠기기", "조수의 정리");
        string reflectionBody = playerReflectionManager != null
            ? playerReflectionManager.BuildReflectionText()
            : "사색 기능이 아직 연결되지 않았습니다.";
        string assistantBody = assistantDiscussionManager != null
            ? assistantDiscussionManager.BuildAssistantSummary()
            : "조수가 아직 연결되지 않았습니다.";

        SetBodies(reflectionBody, assistantBody);
    }

    private void RenderMagicCircleTab()
    {
        SetTitles("마법진 조각", "마법진 판");
        SetBodies(string.Empty, string.Empty);
        SetTextBodiesVisible(false);
        EnsureMagicCircleVisualUI();
        SetMagicCircleVisualVisible(true);
        RefreshMagicCircleVisualUI();
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

    private string BuildConnectionBoardText()
    {
        if (investigationBoardManager == null || investigationBoardManager.Nodes.Count == 0)
        {
            return "아직 연결할 단서가 없습니다. 조사나 탐문으로 단서, 키워드, 발언을 모아야 합니다.";
        }

        ClampBoardSelection();
        ClampBoardScroll();

        StringBuilder builder = new();
        builder.AppendLine("[연결 후보]");
        int nodeCount = investigationBoardManager.Nodes.Count;
        int visibleCount = Mathf.Min(BoardVisibleNodeCount, nodeCount);
        int endIndex = Mathf.Min(_boardScrollIndex + visibleCount, nodeCount);
        builder.AppendLine($"{_boardScrollIndex + 1}-{endIndex} / {nodeCount}");

        for (int i = _boardScrollIndex; i < endIndex; i++)
        {
            InvestigationNode node = investigationBoardManager.Nodes[i];
            string marker = i == _firstBoardNodeIndex ? "A" : i == _secondBoardNodeIndex ? "B" : " ";
            builder.AppendLine($"{marker} {i + 1}. {FormatNodeType(node.Type)} {node.DisplayName}");
        }

        builder.AppendLine();
        builder.AppendLine("[선택]");
        builder.AppendLine($"A: {GetSelectedNodeName(_firstBoardNodeIndex)}");
        builder.AppendLine($"B: {GetSelectedNodeName(_secondBoardNodeIndex)}");
        builder.AppendLine();
        builder.AppendLine(_lastBoardResult);
        builder.AppendLine();
        builder.AppendLine("휠/↑↓: 목록 이동");
        builder.AppendLine("Q/E: A 변경  A/D: B 변경  Enter: 연결");
        return builder.ToString();
    }

    private string BuildHypothesisText()
    {
        if (investigationBoardManager == null || investigationBoardManager.Hypotheses.Count == 0)
        {
            return "아직 생성된 가설 카드가 없습니다. 서로 관련 있어 보이는 단서 두 개를 연결해보세요.";
        }

        StringBuilder builder = new();
        foreach (InvestigationHypothesis hypothesis in investigationBoardManager.Hypotheses)
        {
            builder.AppendLine($"[{hypothesis.Title}]");
            AppendIfNotEmpty(builder, hypothesis.Description);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string BuildMagicCircleSelectionText()
    {
        if (magicCircleInferenceManager == null)
        {
            return "마법진 추리 매니저가 아직 연결되지 않았습니다.";
        }

        IReadOnlyList<MagicCirclePart> mainImages = magicCircleInferenceManager.GetParts(MagicCirclePartType.MainImage);
        IReadOnlyList<MagicCirclePart> supportImages = magicCircleInferenceManager.GetParts(MagicCirclePartType.SupportImage);
        IReadOnlyList<MagicCirclePart> minorPatterns = magicCircleInferenceManager.GetParts(MagicCirclePartType.MinorPattern);
        ClampMagicCircleCursor(minorPatterns.Count);

        StringBuilder builder = new();
        builder.AppendLine("[현재 마법진]");
        builder.AppendLine($"중심그림: {GetSelectedMagicPartName(MagicCirclePartType.MainImage)}");
        builder.AppendLine($"보조그림: {GetSelectedMagicPartName(MagicCirclePartType.SupportImage)}");
        builder.AppendLine($"작은문양: {BuildSelectedMinorPatternNames()}");
        builder.AppendLine();
        builder.AppendLine("[중심그림]");
        AppendMagicPartList(builder, mainImages, magicCircleInferenceManager.SelectedMainImageId, -1);
        builder.AppendLine();
        builder.AppendLine("[보조그림]");
        AppendMagicPartList(builder, supportImages, magicCircleInferenceManager.SelectedSupportImageId, -1);
        builder.AppendLine();
        builder.AppendLine("[작은문양]");
        AppendMagicPartList(builder, minorPatterns, string.Empty, _magicMinorCursorIndex);
        builder.AppendLine();
        builder.AppendLine("M: 마법진 추리 탭");
        builder.AppendLine("Q/E: 중심그림 변경  A/D: 보조그림 변경");
        builder.AppendLine("Z/C: 작은문양 이동  Space: 작은문양 선택");
        builder.AppendLine("Enter: 판정  R: 초기화");
        return builder.ToString();
    }

    private string BuildMagicCircleResultText()
    {
        StringBuilder builder = new();
        builder.AppendLine("[판정 결과]");
        builder.AppendLine(_lastMagicCircleResult.Title);
        AppendIfNotEmpty(builder, _lastMagicCircleResult.Description);
        builder.AppendLine();
        builder.AppendLine("[구조]");
        builder.AppendLine("중심그림: 마법의 핵심 속성");
        builder.AppendLine("보조그림: 마법의 작동 특성");
        builder.AppendLine("작은문양: 조건, 대상, 범위 같은 세부 설정");
        builder.AppendLine();
        builder.AppendLine("이 탭은 임시 MVP입니다. 나중에는 각 조각을 종이 카드처럼 클릭해서 조합하는 UI로 바꿀 수 있습니다.");
        return builder.ToString();
    }

    private void EnsureMagicCircleVisualUI()
    {
        if (_magicLeftRoot != null || leftBodyText == null || rightBodyText == null)
        {
            return;
        }

        Transform leftParent = leftBodyText.transform.parent;
        Transform rightParent = rightBodyText.transform.parent;
        TMP_FontAsset font = leftBodyText.font;

        _magicLeftRoot = CreatePanelRoot(leftParent, "MagicCirclePartSelectionRoot");
        _magicRightRoot = CreatePanelRoot(rightParent, "MagicCirclePreviewRoot");

        RectTransform selectorRow = CreateRect("PartTypeSelectors", _magicLeftRoot.transform);
        selectorRow.anchorMin = new Vector2(0f, 1f);
        selectorRow.anchorMax = new Vector2(1f, 1f);
        selectorRow.pivot = new Vector2(0.5f, 1f);
        selectorRow.anchoredPosition = new Vector2(0f, -10f);
        selectorRow.sizeDelta = new Vector2(0f, 54f);
        HorizontalLayoutGroup selectorLayout = selectorRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        selectorLayout.spacing = 12f;
        selectorLayout.childForceExpandWidth = true;
        selectorLayout.childForceExpandHeight = true;
        selectorLayout.padding = new RectOffset(6, 6, 0, 0);

        _magicMainSelectorText = CreateMagicButton(selectorRow, "MainPatternButton", "메인문양", font, () => SelectMagicPartType(MagicCirclePartType.MainImage));
        _magicSupportSelectorText = CreateMagicButton(selectorRow, "SupportPatternButton", "보조문양", font, () => SelectMagicPartType(MagicCirclePartType.SupportImage));
        _magicMinorSelectorText = CreateMagicButton(selectorRow, "MinorPatternButton", "작은문양", font, () => SelectMagicPartType(MagicCirclePartType.MinorPattern));

        RectTransform scrollRoot = CreateRect("MagicPartScroll", _magicLeftRoot.transform);
        scrollRoot.anchorMin = new Vector2(0f, 0f);
        scrollRoot.anchorMax = new Vector2(1f, 1f);
        scrollRoot.offsetMin = new Vector2(6f, 8f);
        scrollRoot.offsetMax = new Vector2(-6f, -76f);
        Image scrollImage = scrollRoot.gameObject.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.03f);
        scrollImage.raycastTarget = true;

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        _magicPartContent = CreateRect("Content", viewport);
        _magicPartContent.anchorMin = new Vector2(0f, 1f);
        _magicPartContent.anchorMax = new Vector2(1f, 1f);
        _magicPartContent.pivot = new Vector2(0.5f, 1f);
        _magicPartContent.anchoredPosition = Vector2.zero;
        _magicPartContent.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = _magicPartContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170f, 220f);
        grid.spacing = new Vector2(14f, 16f);
        grid.padding = new RectOffset(18, 18, 10, 10);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        ContentSizeFitter fitter = _magicPartContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = _magicPartContent;

        _magicPreviewText = CreateText(_magicRightRoot.transform, "MagicCirclePreviewText", font, 24, TextAlignmentOptions.Center);
        RectTransform previewRect = _magicPreviewText.rectTransform;
        previewRect.anchorMin = new Vector2(0.06f, 0.18f);
        previewRect.anchorMax = new Vector2(0.94f, 0.94f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;

        RectTransform actionRow = CreateRect("MagicCircleActions", _magicRightRoot.transform);
        actionRow.anchorMin = new Vector2(0f, 0f);
        actionRow.anchorMax = new Vector2(1f, 0f);
        actionRow.pivot = new Vector2(0.5f, 0f);
        actionRow.anchoredPosition = new Vector2(0f, 16f);
        actionRow.sizeDelta = new Vector2(0f, 46f);
        HorizontalLayoutGroup actionLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 12f;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = true;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;

        CreateMagicButton(actionRow, "InferMagicButton", "판정하기", font, InferMagicCircle);
        CreateMagicButton(actionRow, "ClearMagicButton", "초기화", font, ClearMagicCircle);

        _magicResultText = CreateText(_magicRightRoot.transform, "MagicCircleResultText", font, 18, TextAlignmentOptions.Center);
        RectTransform resultRect = _magicResultText.rectTransform;
        resultRect.anchorMin = new Vector2(0.08f, 0.06f);
        resultRect.anchorMax = new Vector2(0.92f, 0.16f);
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;
    }

    private void RefreshMagicCircleVisualUI()
    {
        if (magicCircleInferenceManager == null || _magicPartContent == null)
        {
            return;
        }

        UpdateMagicSelectorLabels();
        RebuildMagicPartButtons();
        UpdateMagicPreview();
    }

    private void SelectMagicPartType(MagicCirclePartType type)
    {
        _activeMagicPartType = type;
        RefreshMagicCircleVisualUI();
    }

    private void SelectMagicCirclePart(MagicCirclePart part)
    {
        if (part == null || magicCircleInferenceManager == null)
        {
            return;
        }

        if (part.Type == MagicCirclePartType.MainImage)
        {
            magicCircleInferenceManager.SelectMainImage(part.PartId);
        }
        else if (part.Type == MagicCirclePartType.SupportImage)
        {
            magicCircleInferenceManager.SelectSupportImage(part.PartId);
        }
        else
        {
            magicCircleInferenceManager.SelectMinorPattern(part.PartId);
        }

        RefreshMagicCircleVisualUI();
    }

    private void InferMagicCircle()
    {
        if (magicCircleInferenceManager == null)
        {
            return;
        }

        _lastMagicCircleResult = magicCircleInferenceManager.Infer();
        UpdateMagicPreview();
    }

    private void ClearMagicCircle()
    {
        if (magicCircleInferenceManager == null)
        {
            return;
        }

        magicCircleInferenceManager.ClearSelection();
        _lastMagicCircleResult = MagicCircleInferenceResult.Failed("마법진 조각을 고른 뒤 판정해보자.");
        RefreshMagicCircleVisualUI();
    }

    private void UpdateMagicSelectorLabels()
    {
        if (_magicMainSelectorText != null)
        {
            _magicMainSelectorText.text = BuildSelectorText("메인문양", GetSelectedMagicPartName(MagicCirclePartType.MainImage), MagicCirclePartType.MainImage);
        }

        if (_magicSupportSelectorText != null)
        {
            _magicSupportSelectorText.text = BuildSelectorText("보조문양", GetSelectedMagicPartName(MagicCirclePartType.SupportImage), MagicCirclePartType.SupportImage);
        }

        if (_magicMinorSelectorText != null)
        {
            _magicMinorSelectorText.text = BuildSelectorText("작은문양", BuildSelectedMinorPatternNames(), MagicCirclePartType.MinorPattern);
        }
    }

    private void RebuildMagicPartButtons()
    {
        for (int i = _magicPartContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_magicPartContent.GetChild(i).gameObject);
        }

        IReadOnlyList<MagicCirclePart> parts = magicCircleInferenceManager.GetParts(_activeMagicPartType);
        for (int i = 0; i < parts.Count; i++)
        {
            MagicCirclePart part = parts[i];
            TMP_Text label = CreateMagicButton(_magicPartContent, $"MagicPart_{part.PartId}", BuildMagicPartButtonText(part), leftBodyText != null ? leftBodyText.font : null, () => SelectMagicCirclePart(part));
            Image image = label.GetComponentInParent<Image>();
            if (image != null)
            {
                image.color = IsMagicPartSelected(part) ? new Color(0.48f, 0.42f, 0.28f, 0.95f) : new Color(0.88f, 0.83f, 0.68f, 0.92f);
            }
        }
    }

    private void UpdateMagicPreview()
    {
        if (_magicPreviewText == null || magicCircleInferenceManager == null)
        {
            return;
        }

        string main = GetSelectedMagicPartName(MagicCirclePartType.MainImage);
        string support = GetSelectedMagicPartName(MagicCirclePartType.SupportImage);
        string minor = BuildSelectedMinorPatternNames();
        _magicPreviewText.text =
            $"<size=260>◎</size>\n" +
            $"<size=32>{main}</size>\n" +
            $"<size=22>{support}</size>\n" +
            $"<size=18>{minor}</size>";

        if (_magicResultText != null)
        {
            _magicResultText.text = $"{_lastMagicCircleResult.Title}\n{_lastMagicCircleResult.Description}";
        }
    }

    private string BuildSelectorText(string title, string value, MagicCirclePartType type)
    {
        string marker = _activeMagicPartType == type ? "▶ " : string.Empty;
        return $"{marker}{title}\n<size=15>{value}</size>";
    }

    private string BuildMagicPartButtonText(MagicCirclePart part)
    {
        string selected = IsMagicPartSelected(part) ? "✓ " : string.Empty;
        return $"{selected}{part.DisplayName}\n\n<size=46>◎</size>\n\n<size=13>{part.Description}</size>";
    }

    private bool IsMagicPartSelected(MagicCirclePart part)
    {
        if (part == null)
        {
            return false;
        }

        return part.Type switch
        {
            MagicCirclePartType.MainImage => part.MatchesId(magicCircleInferenceManager.SelectedMainImageId),
            MagicCirclePartType.SupportImage => part.MatchesId(magicCircleInferenceManager.SelectedSupportImageId),
            MagicCirclePartType.MinorPattern => ContainsMagicPartId(magicCircleInferenceManager.SelectedMinorPatternIds, part.PartId),
            _ => false
        };
    }

    private void SetTextBodiesVisible(bool visible)
    {
        if (leftBodyText != null)
        {
            leftBodyText.gameObject.SetActive(visible);
        }

        if (rightBodyText != null)
        {
            rightBodyText.gameObject.SetActive(visible);
        }
    }

    private void SetMagicCircleVisualVisible(bool visible)
    {
        if (_magicLeftRoot != null)
        {
            _magicLeftRoot.SetActive(visible);
        }

        if (_magicRightRoot != null)
        {
            _magicRightRoot.SetActive(visible);
        }
    }

    private static GameObject CreatePanelRoot(Transform parent, string objectName)
    {
        GameObject rootObject = new(objectName);
        rootObject.transform.SetParent(parent, false);
        RectTransform rect = rootObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f);
        rect.offsetMax = new Vector2(-10f, -10f);
        return rootObject;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }

    private static TMP_Text CreateText(Transform parent, string objectName, TMP_FontAsset font, int fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = new Color(0.19f, 0.17f, 0.14f, 0.92f);
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private static TMP_Text CreateMagicButton(RectTransform parent, string objectName, string label, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new(objectName);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.88f, 0.83f, 0.68f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = new("Label");
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 6f);
        textRect.offsetMax = new Vector2(-8f, -6f);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = label;
        text.fontSize = 17f;
        text.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 150f;
        layout.preferredHeight = 46f;

        return text;
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

        if (playerReflectionManager == null)
        {
            playerReflectionManager = FindFirstObjectByType<PlayerReflectionManager>();
        }

        if (playerReflectionManager == null)
        {
            playerReflectionManager = gameObject.AddComponent<PlayerReflectionManager>();
        }

        if (investigationBoardManager == null)
        {
            investigationBoardManager = FindFirstObjectByType<InvestigationBoardManager>();
        }

        if (investigationBoardManager == null)
        {
            investigationBoardManager = gameObject.AddComponent<InvestigationBoardManager>();
        }

        if (magicCircleInferenceManager == null)
        {
            magicCircleInferenceManager = FindFirstObjectByType<MagicCircleInferenceManager>();
        }

        if (magicCircleInferenceManager == null)
        {
            magicCircleInferenceManager = gameObject.AddComponent<MagicCircleInferenceManager>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    private void BindButtons()
    {
        ResolveMagicCircleTabButton();

        BindButton(evidenceTabButton, ShowEvidenceTab);
        BindButton(npcTabButton, ShowNpcTab);
        BindButton(dialogueLogTabButton, ShowDialogueLogTab);
        BindButton(reflectionTabButton, ShowReflectionTab);
        BindButton(assistantTabButton, ShowAssistantTab);
        BindButton(magicCircleTabButton, ShowMagicCircleTab);
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
        TmpTextPrewarmUtility.Prewarm(magicCircleTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(closeButton, TmpPrewarmText);
    }

    private void ResolveMagicCircleTabButton()
    {
        if (magicCircleTabButton != null)
        {
            return;
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                continue;
            }

            string text = label.text.Trim();
            if (text == "마법진 조사" || text == "마법진 추리")
            {
                magicCircleTabButton = button;
                return;
            }
        }
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

    private void HandleBoardInput()
    {
        UpdateUiNavigationSuppression();

        if (investigationBoardManager == null || investigationBoardManager.Nodes.Count == 0)
        {
            return;
        }

        bool changed = false;
        int scrollDelta = GetBoardScrollDelta();
        if (scrollDelta != 0)
        {
            _boardScrollIndex -= scrollDelta;
            changed = true;
        }
        else if (WasBoardKeyPressed(BoardKey.PreviousFirst))
        {
            _firstBoardNodeIndex--;
            EnsureBoardSelectionVisible(_firstBoardNodeIndex);
            changed = true;
        }
        else if (WasBoardKeyPressed(BoardKey.NextFirst))
        {
            _firstBoardNodeIndex++;
            EnsureBoardSelectionVisible(_firstBoardNodeIndex);
            changed = true;
        }
        else if (WasBoardKeyPressed(BoardKey.PreviousSecond))
        {
            _secondBoardNodeIndex--;
            EnsureBoardSelectionVisible(_secondBoardNodeIndex);
            changed = true;
        }
        else if (WasBoardKeyPressed(BoardKey.NextSecond))
        {
            _secondBoardNodeIndex++;
            EnsureBoardSelectionVisible(_secondBoardNodeIndex);
            changed = true;
        }
        else if (WasBoardKeyPressed(BoardKey.Connect))
        {
            ClampBoardSelection();
            InvestigationNode first = investigationBoardManager.Nodes[_firstBoardNodeIndex];
            InvestigationNode second = investigationBoardManager.Nodes[_secondBoardNodeIndex];
            LinkResult result = investigationBoardManager.TryConnect(first.NodeId, second.NodeId);
            _lastBoardResult = result.Message;
            changed = true;
        }

        if (changed)
        {
            ClampBoardSelection();
            ClampBoardScroll();
            Refresh();
        }
    }

    private void HandleMagicCircleInput()
    {
        UpdateUiNavigationSuppression();

        if (magicCircleInferenceManager == null)
        {
            return;
        }

        bool changed = false;
        if (WasMagicKeyPressed(MagicKey.PreviousMain))
        {
            SelectAdjacentMagicPart(MagicCirclePartType.MainImage, -1);
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.NextMain))
        {
            SelectAdjacentMagicPart(MagicCirclePartType.MainImage, 1);
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.PreviousSupport))
        {
            SelectAdjacentMagicPart(MagicCirclePartType.SupportImage, -1);
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.NextSupport))
        {
            SelectAdjacentMagicPart(MagicCirclePartType.SupportImage, 1);
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.PreviousMinor))
        {
            _magicMinorCursorIndex--;
            ClampMagicCircleCursor(magicCircleInferenceManager.GetParts(MagicCirclePartType.MinorPattern).Count);
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.NextMinor))
        {
            _magicMinorCursorIndex++;
            ClampMagicCircleCursor(magicCircleInferenceManager.GetParts(MagicCirclePartType.MinorPattern).Count);
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.ToggleMinor))
        {
            IReadOnlyList<MagicCirclePart> minorPatterns = magicCircleInferenceManager.GetParts(MagicCirclePartType.MinorPattern);
            if (minorPatterns.Count > 0)
            {
                ClampMagicCircleCursor(minorPatterns.Count);
                magicCircleInferenceManager.SelectMinorPattern(minorPatterns[_magicMinorCursorIndex].PartId);
                changed = true;
            }
        }
        else if (WasMagicKeyPressed(MagicKey.Infer))
        {
            _lastMagicCircleResult = magicCircleInferenceManager.Infer();
            changed = true;
        }
        else if (WasMagicKeyPressed(MagicKey.Clear))
        {
            magicCircleInferenceManager.ClearSelection();
            _lastMagicCircleResult = MagicCircleInferenceResult.Failed("마법진 조각을 고른 뒤 판정해보자.");
            changed = true;
        }

        if (changed)
        {
            Refresh();
        }
    }

    private void SelectAdjacentMagicPart(MagicCirclePartType type, int direction)
    {
        IReadOnlyList<MagicCirclePart> parts = magicCircleInferenceManager.GetParts(type);
        if (parts.Count == 0)
        {
            return;
        }

        string selectedId = type == MagicCirclePartType.MainImage
            ? magicCircleInferenceManager.SelectedMainImageId
            : magicCircleInferenceManager.SelectedSupportImageId;
        int selectedIndex = FindMagicPartIndex(parts, selectedId);
        int nextIndex = WrapIndex(selectedIndex + direction, parts.Count);

        if (type == MagicCirclePartType.MainImage)
        {
            magicCircleInferenceManager.SelectMainImage(parts[nextIndex].PartId);
        }
        else if (type == MagicCirclePartType.SupportImage)
        {
            magicCircleInferenceManager.SelectSupportImage(parts[nextIndex].PartId);
        }
    }

    private string GetSelectedMagicPartName(MagicCirclePartType type)
    {
        return magicCircleInferenceManager.TryGetSelectedPart(type, out MagicCirclePart selectedPart)
            ? selectedPart.DisplayName
            : "선택 안 됨";
    }

    private string BuildSelectedMinorPatternNames()
    {
        IReadOnlyList<string> selectedIds = magicCircleInferenceManager.SelectedMinorPatternIds;
        if (selectedIds.Count == 0)
        {
            return "선택 안 됨";
        }

        IReadOnlyList<MagicCirclePart> minorPatterns = magicCircleInferenceManager.GetParts(MagicCirclePartType.MinorPattern);
        StringBuilder builder = new();
        for (int i = 0; i < selectedIds.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            int partIndex = FindMagicPartIndex(minorPatterns, selectedIds[i]);
            builder.Append(partIndex >= 0 ? minorPatterns[partIndex].DisplayName : selectedIds[i]);
        }

        return builder.ToString();
    }

    private void AppendMagicPartList(StringBuilder builder, IReadOnlyList<MagicCirclePart> parts, string selectedId, int cursorIndex)
    {
        if (parts.Count == 0)
        {
            builder.AppendLine("- 등록된 조각 없음");
            return;
        }

        IReadOnlyList<string> selectedMinorPatterns = magicCircleInferenceManager.SelectedMinorPatternIds;
        for (int i = 0; i < parts.Count; i++)
        {
            MagicCirclePart part = parts[i];
            bool selected = !string.IsNullOrWhiteSpace(selectedId) && part.MatchesId(selectedId);
            bool minorSelected = ContainsMagicPartId(selectedMinorPatterns, part.PartId);
            string marker = i == cursorIndex ? ">" : selected || minorSelected ? "*" : "-";
            builder.AppendLine($"{marker} {part.DisplayName}: {part.Description}");
        }
    }

    private void ClampMagicCircleCursor(int count)
    {
        _magicMinorCursorIndex = count <= 0 ? 0 : WrapIndex(_magicMinorCursorIndex, count);
    }

    private static int FindMagicPartIndex(IReadOnlyList<MagicCirclePart> parts, string partId)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].MatchesId(partId))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ContainsMagicPartId(IReadOnlyList<string> partIds, string partId)
    {
        for (int i = 0; i < partIds.Count; i++)
        {
            if (string.Equals(partIds[i], partId, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ClampBoardSelection()
    {
        int count = investigationBoardManager != null ? investigationBoardManager.Nodes.Count : 0;
        if (count <= 0)
        {
            _firstBoardNodeIndex = 0;
            _secondBoardNodeIndex = 0;
            _boardScrollIndex = 0;
            return;
        }

        _firstBoardNodeIndex = WrapIndex(_firstBoardNodeIndex, count);
        _secondBoardNodeIndex = WrapIndex(_secondBoardNodeIndex, count);

        if (count > 1 && _firstBoardNodeIndex == _secondBoardNodeIndex)
        {
            _secondBoardNodeIndex = WrapIndex(_secondBoardNodeIndex + 1, count);
        }
    }

    private void ClampBoardScroll()
    {
        int count = investigationBoardManager != null ? investigationBoardManager.Nodes.Count : 0;
        int maxScrollIndex = Mathf.Max(0, count - BoardVisibleNodeCount);
        _boardScrollIndex = Mathf.Clamp(_boardScrollIndex, 0, maxScrollIndex);
    }

    private void EnsureBoardSelectionVisible(int index)
    {
        int count = investigationBoardManager != null ? investigationBoardManager.Nodes.Count : 0;
        if (count <= 0)
        {
            _boardScrollIndex = 0;
            return;
        }

        index = WrapIndex(index, count);
        if (index < _boardScrollIndex)
        {
            _boardScrollIndex = index;
        }
        else if (index >= _boardScrollIndex + BoardVisibleNodeCount)
        {
            _boardScrollIndex = index - BoardVisibleNodeCount + 1;
        }
    }

    private string GetSelectedNodeName(int index)
    {
        if (investigationBoardManager == null || investigationBoardManager.Nodes.Count == 0)
        {
            return "없음";
        }

        index = WrapIndex(index, investigationBoardManager.Nodes.Count);
        InvestigationNode node = investigationBoardManager.Nodes[index];
        return $"{FormatNodeType(node.Type)} {node.DisplayName}";
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }

    private static string FormatNodeType(InvestigationNodeType type)
    {
        return type switch
        {
            InvestigationNodeType.Evidence => "[단서]",
            InvestigationNodeType.Keyword => "[키워드]",
            InvestigationNodeType.Dialogue => "[발언]",
            InvestigationNodeType.Observation => "[관찰]",
            InvestigationNodeType.Hypothesis => "[가설]",
            _ => "[정보]"
        };
    }

    private void HideOverlappedObjects()
    {
        if (hideWhileOpen == null || hideWhileOpen.Length == 0)
        {
            return;
        }

        foreach (GameObject target in hideWhileOpen)
        {
            if (ShouldSkipHiddenObject(target) || _hiddenObjectStates.ContainsKey(target))
            {
                continue;
            }

            _hiddenObjectStates.Add(target, target.activeSelf);
            target.SetActive(false);
        }
    }

    private void RestoreHiddenObjects()
    {
        if (_hiddenObjectStates.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<GameObject, bool> entry in _hiddenObjectStates)
        {
            if (entry.Key != null)
            {
                entry.Key.SetActive(entry.Value);
            }
        }

        _hiddenObjectStates.Clear();
    }

    private bool ShouldSkipHiddenObject(GameObject target)
    {
        if (target == null || target == gameObject || target == panelRoot)
        {
            return true;
        }

        if (panelRoot == null)
        {
            return false;
        }

        Transform panelTransform = panelRoot.transform;
        Transform targetTransform = target.transform;
        return targetTransform.IsChildOf(panelTransform) || panelTransform.IsChildOf(targetTransform);
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

    private void UpdateUiNavigationSuppression()
    {
        if (!suppressUiNavigationWhileConnectingClues ||
            !IsVisible ||
            (_currentTab != NotebookTab.Reflection && _currentTab != NotebookTab.MagicCircle))
        {
            RestoreUiNavigation();
            return;
        }

        SuppressUiNavigation();
    }

    private void SuppressUiNavigation()
    {
        if (_eventSystem == null)
        {
            _eventSystem = EventSystem.current;
        }

        if (_eventSystem == null)
        {
            return;
        }

        if (_isSuppressingUiNavigation)
        {
            if (_eventSystem.currentSelectedGameObject != null)
            {
                _eventSystem.SetSelectedGameObject(null);
            }

            return;
        }

        _previousSendNavigationEvents = _eventSystem.sendNavigationEvents;
        _previousSelectedObject = _eventSystem.currentSelectedGameObject;
        _eventSystem.sendNavigationEvents = false;
        _eventSystem.SetSelectedGameObject(null);
        _isSuppressingUiNavigation = true;
    }

    private void RestoreUiNavigation()
    {
        if (!_isSuppressingUiNavigation)
        {
            return;
        }

        if (_eventSystem != null)
        {
            _eventSystem.sendNavigationEvents = _previousSendNavigationEvents;
            if (_previousSelectedObject != null && _previousSelectedObject.activeInHierarchy)
            {
                _eventSystem.SetSelectedGameObject(_previousSelectedObject);
            }
        }

        _previousSelectedObject = null;
        _isSuppressingUiNavigation = false;
    }

    private enum BoardKey
    {
        PreviousFirst,
        NextFirst,
        PreviousSecond,
        NextSecond,
        Connect,
        ScrollUp,
        ScrollDown
    }

    private enum MagicKey
    {
        PreviousMain,
        NextMain,
        PreviousSupport,
        NextSupport,
        PreviousMinor,
        NextMinor,
        ToggleMinor,
        Infer,
        Clear
    }

    private static int GetBoardScrollDelta()
    {
        int delta = 0;
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (scrollY > 0f)
            {
                delta++;
            }
            else if (scrollY < 0f)
            {
                delta--;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        float scrollY = Input.mouseScrollDelta.y;
        if (scrollY > 0f)
        {
            delta++;
        }
        else if (scrollY < 0f)
        {
            delta--;
        }
#endif

        if (WasBoardKeyPressed(BoardKey.ScrollUp))
        {
            delta++;
        }
        else if (WasBoardKeyPressed(BoardKey.ScrollDown))
        {
            delta--;
        }

        return delta;
    }

    private static bool WasBoardKeyPressed(BoardKey key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return key switch
        {
            BoardKey.PreviousFirst => keyboard.qKey.wasPressedThisFrame,
            BoardKey.NextFirst => keyboard.eKey.wasPressedThisFrame,
            BoardKey.PreviousSecond => keyboard.aKey.wasPressedThisFrame,
            BoardKey.NextSecond => keyboard.dKey.wasPressedThisFrame,
            BoardKey.Connect => keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame,
            BoardKey.ScrollUp => keyboard.upArrowKey.wasPressedThisFrame,
            BoardKey.ScrollDown => keyboard.downArrowKey.wasPressedThisFrame,
            _ => false
        };
#elif ENABLE_LEGACY_INPUT_MANAGER
        return key switch
        {
            BoardKey.PreviousFirst => Input.GetKeyDown(KeyCode.Q),
            BoardKey.NextFirst => Input.GetKeyDown(KeyCode.E),
            BoardKey.PreviousSecond => Input.GetKeyDown(KeyCode.A),
            BoardKey.NextSecond => Input.GetKeyDown(KeyCode.D),
            BoardKey.Connect => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter),
            BoardKey.ScrollUp => Input.GetKeyDown(KeyCode.UpArrow),
            BoardKey.ScrollDown => Input.GetKeyDown(KeyCode.DownArrow),
            _ => false
        };
#else
        return false;
#endif
    }

    private static bool WasMagicCircleTabPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.mKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.M);
#else
        return false;
#endif
    }

    private static bool WasMagicKeyPressed(MagicKey key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return key switch
        {
            MagicKey.PreviousMain => keyboard.qKey.wasPressedThisFrame,
            MagicKey.NextMain => keyboard.eKey.wasPressedThisFrame,
            MagicKey.PreviousSupport => keyboard.aKey.wasPressedThisFrame,
            MagicKey.NextSupport => keyboard.dKey.wasPressedThisFrame,
            MagicKey.PreviousMinor => keyboard.zKey.wasPressedThisFrame,
            MagicKey.NextMinor => keyboard.cKey.wasPressedThisFrame,
            MagicKey.ToggleMinor => keyboard.spaceKey.wasPressedThisFrame,
            MagicKey.Infer => keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame,
            MagicKey.Clear => keyboard.rKey.wasPressedThisFrame,
            _ => false
        };
#elif ENABLE_LEGACY_INPUT_MANAGER
        return key switch
        {
            MagicKey.PreviousMain => Input.GetKeyDown(KeyCode.Q),
            MagicKey.NextMain => Input.GetKeyDown(KeyCode.E),
            MagicKey.PreviousSupport => Input.GetKeyDown(KeyCode.A),
            MagicKey.NextSupport => Input.GetKeyDown(KeyCode.D),
            MagicKey.PreviousMinor => Input.GetKeyDown(KeyCode.Z),
            MagicKey.NextMinor => Input.GetKeyDown(KeyCode.C),
            MagicKey.ToggleMinor => Input.GetKeyDown(KeyCode.Space),
            MagicKey.Infer => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter),
            MagicKey.Clear => Input.GetKeyDown(KeyCode.R),
            _ => false
        };
#else
        return false;
#endif
    }
}
