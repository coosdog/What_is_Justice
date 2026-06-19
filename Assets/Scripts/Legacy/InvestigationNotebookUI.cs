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
        Mantra
    }

    private const string TmpPrewarmText = "수사노트 단서 NPC 대화 기록 단서연결 가설 카드 사색에 잠기기 할아버지의 조언 만트라 조사 주 만트라 보조 만트라 장식 만트라 기본 관찰 예리한 시야 미세한 청각 침묵의 응시";
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
    [SerializeField] private Button mantraTabButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool showAssistantTab = false;

    [Header("Data")]
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private NpcProfileRegistry npcProfileRegistry;
    [SerializeField] private AssistantDiscussionManager assistantDiscussionManager;
    [SerializeField] private PlayerReflectionManager playerReflectionManager;
    [SerializeField] private InvestigationBoardManager investigationBoardManager;
    [SerializeField] private MantraInferenceManager mantraInferenceManager;
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
    private int _mantraMinorCursorIndex;
    private MantraInferenceResult _lastMantraResult = MantraInferenceResult.Failed("만트라 조각을 고른 뒤 판정해보자.");
    private EventSystem _eventSystem;
    private GameObject _previousSelectedObject;
    private bool _previousSendNavigationEvents;
    private bool _isSuppressingUiNavigation;
    private MantraPartType _activeMantraPartType = MantraPartType.MainImage;
    private GameObject _mantraLeftRoot;
    private GameObject _mantraRightRoot;
    private RectTransform _mantraPartContent;
    private TMP_Text _mantraMainSelectorText;
    private TMP_Text _mantraSupportSelectorText;
    private TMP_Text _mantraMinorSelectorText;
    private TMP_Text _mantraPreviewText;
    private TMP_Text _mantraResultText;
    private Image _mantraMainPreviewImage;
    private Image _mantraSubPreviewImage;
    private Image _mantraGuidePreviewImage;

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

        if (IsVisible && _currentTab == NotebookTab.Mantra)
        {
            HandleMantraInput();
        }

        if (IsVisible && WasMantraTabPressedThisFrame())
        {
            SelectTab(NotebookTab.Mantra);
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
    public void ShowAssistantTab()
    {
        if (showAssistantTab)
        {
            SelectTab(NotebookTab.Assistant);
        }
    }
    public void ShowMantraTab() => SelectTab(NotebookTab.Mantra);

    private void SelectTab(NotebookTab tab)
    {
        if (tab == NotebookTab.Assistant && !showAssistantTab)
        {
            tab = NotebookTab.Evidence;
        }

        _currentTab = tab;
        Refresh();
        UpdateUiNavigationSuppression();
    }

    private void Refresh()
    {
        if (_currentTab != NotebookTab.Mantra)
        {
            SetMantraVisualVisible(false);
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
            case NotebookTab.Mantra:
                RenderMantraTab();
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
        SetTitles("사색에 잠기기", "할아버지의 조언");
        string reflectionBody = playerReflectionManager != null
            ? playerReflectionManager.BuildReflectionText()
            : "사색 기능이 아직 연결되지 않았습니다.";
        string assistantBody = assistantDiscussionManager != null
            ? assistantDiscussionManager.BuildAssistantSummary()
            : "할아버지의 조언 기능이 아직 연결되지 않았습니다.";

        SetBodies(reflectionBody, assistantBody);
    }

    private void RenderMantraTab()
    {
        SetTitles("만트라 조각", "만트라 판");
        SetBodies(string.Empty, string.Empty);
        SetTextBodiesVisible(false);
        EnsureMantraVisualUI();
        SetMantraVisualVisible(true);
        RefreshMantraVisualUI();
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

    private string BuildMantraSelectionText()
    {
        if (mantraInferenceManager == null)
        {
            return "만트라 추리 매니저가 아직 연결되지 않았습니다.";
        }

        IReadOnlyList<MantraPart> mainImages = mantraInferenceManager.GetParts(MantraPartType.MainImage);
        IReadOnlyList<MantraPart> supportImages = mantraInferenceManager.GetParts(MantraPartType.SupportImage);
        IReadOnlyList<MantraPart> minorPatterns = mantraInferenceManager.GetParts(MantraPartType.MinorPattern);
        ClampMantraCursor(minorPatterns.Count);

        StringBuilder builder = new();
        builder.AppendLine("[현재 만트라]");
        builder.AppendLine($"주 만트라: {GetSelectedMantraPartName(MantraPartType.MainImage)}");
        builder.AppendLine($"보조 만트라: {GetSelectedMantraPartName(MantraPartType.SupportImage)}");
        builder.AppendLine($"장식 만트라: {BuildSelectedMinorPatternNames()}");
        builder.AppendLine();
        builder.AppendLine("[주 만트라]");
        AppendMantraPartList(builder, mainImages, mantraInferenceManager.SelectedMainImageId, -1);
        builder.AppendLine();
        builder.AppendLine("[보조 만트라]");
        AppendMantraPartList(builder, supportImages, mantraInferenceManager.SelectedSupportImageId, -1);
        builder.AppendLine();
        builder.AppendLine("[장식 만트라]");
        AppendMantraPartList(builder, minorPatterns, string.Empty, _mantraMinorCursorIndex);
        builder.AppendLine();
        builder.AppendLine("M: 만트라 조사 탭");
        builder.AppendLine("Q/E: 주 만트라 변경  A/D: 보조 만트라 변경");
        builder.AppendLine("Z/C: 장식 만트라 이동  Space: 장식 만트라 선택");
        builder.AppendLine("Enter: 판정  R: 초기화");
        return builder.ToString();
    }

    private string BuildMantraResultText()
    {
        StringBuilder builder = new();
        builder.AppendLine("[판정 결과]");
        builder.AppendLine(_lastMantraResult.Title);
        AppendIfNotEmpty(builder, _lastMantraResult.Description);
        builder.AppendLine();
        builder.AppendLine("[구조]");
        builder.AppendLine("주 만트라: 마법의 핵심 속성");
        builder.AppendLine("보조 만트라: 마법의 작동 특성");
        builder.AppendLine("장식 만트라: 조건, 대상, 범위 같은 세부 설정");
        builder.AppendLine();
        builder.AppendLine("이 탭은 임시 MVP입니다. 나중에는 각 조각을 종이 카드처럼 클릭해서 조합하는 UI로 바꿀 수 있습니다.");
        return builder.ToString();
    }

    private void EnsureMantraVisualUI()
    {
        if (_mantraLeftRoot != null || leftBodyText == null || rightBodyText == null)
        {
            return;
        }

        Transform leftParent = leftBodyText.transform.parent;
        Transform rightParent = rightBodyText.transform.parent;
        TMP_FontAsset font = leftBodyText.font;

        _mantraLeftRoot = CreatePanelRoot(leftParent, "MantraPartSelectionRoot");
        _mantraRightRoot = CreatePanelRoot(rightParent, "MantraPreviewRoot");

        RectTransform selectorRow = CreateRect("PartTypeSelectors", _mantraLeftRoot.transform);
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

        _mantraMainSelectorText = CreateMantraButton(selectorRow, "MainMantraButton", "주 만트라", font, () => SelectMantraPartType(MantraPartType.MainImage));
        _mantraSupportSelectorText = CreateMantraButton(selectorRow, "SubMantraButton", "보조 만트라", font, () => SelectMantraPartType(MantraPartType.SupportImage));
        _mantraMinorSelectorText = CreateMantraButton(selectorRow, "GuideMantraButton", "장식 만트라", font, () => SelectMantraPartType(MantraPartType.MinorPattern));

        RectTransform scrollRoot = CreateRect("MantraPartScroll", _mantraLeftRoot.transform);
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

        _mantraPartContent = CreateRect("Content", viewport);
        _mantraPartContent.anchorMin = new Vector2(0f, 1f);
        _mantraPartContent.anchorMax = new Vector2(1f, 1f);
        _mantraPartContent.pivot = new Vector2(0.5f, 1f);
        _mantraPartContent.anchoredPosition = Vector2.zero;
        _mantraPartContent.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = _mantraPartContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170f, 220f);
        grid.spacing = new Vector2(14f, 16f);
        grid.padding = new RectOffset(18, 18, 10, 10);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        ContentSizeFitter fitter = _mantraPartContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = _mantraPartContent;

        RectTransform previewStack = CreateRect("MantraPreviewStack", _mantraRightRoot.transform);
        previewStack.anchorMin = new Vector2(0.16f, 0.32f);
        previewStack.anchorMax = new Vector2(0.84f, 0.90f);
        previewStack.offsetMin = Vector2.zero;
        previewStack.offsetMax = Vector2.zero;
        _mantraGuidePreviewImage = CreatePreviewLayer(previewStack, "GuideMantraLayer");
        _mantraSubPreviewImage = CreatePreviewLayer(previewStack, "SubMantraLayer");
        _mantraMainPreviewImage = CreatePreviewLayer(previewStack, "MainMantraLayer");

        _mantraPreviewText = CreateText(_mantraRightRoot.transform, "MantraPreviewText", font, 24, TextAlignmentOptions.Center);
        RectTransform previewRect = _mantraPreviewText.rectTransform;
        previewRect.anchorMin = new Vector2(0.08f, 0.18f);
        previewRect.anchorMax = new Vector2(0.92f, 0.30f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;

        RectTransform actionRow = CreateRect("MantraActions", _mantraRightRoot.transform);
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

        CreateMantraButton(actionRow, "InferMantraButton", "판정하기", font, InferMantra);
        CreateMantraButton(actionRow, "ClearMantraButton", "초기화", font, ClearMantra);

        _mantraResultText = CreateText(_mantraRightRoot.transform, "MantraResultText", font, 18, TextAlignmentOptions.Center);
        RectTransform resultRect = _mantraResultText.rectTransform;
        resultRect.anchorMin = new Vector2(0.08f, 0.06f);
        resultRect.anchorMax = new Vector2(0.92f, 0.16f);
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;
    }

    private void RefreshMantraVisualUI()
    {
        if (mantraInferenceManager == null || _mantraPartContent == null)
        {
            return;
        }

        UpdateMantraSelectorLabels();
        RebuildMantraPartButtons();
        UpdateMantraPreview();
    }

    private void SelectMantraPartType(MantraPartType type)
    {
        _activeMantraPartType = type;
        RefreshMantraVisualUI();
    }

    private void SelectMantraPart(MantraPart part)
    {
        if (part == null || mantraInferenceManager == null)
        {
            return;
        }

        if (part.Type == MantraPartType.MainImage)
        {
            mantraInferenceManager.SelectMainImage(part.PartId);
        }
        else if (part.Type == MantraPartType.SupportImage)
        {
            mantraInferenceManager.SelectSupportImage(part.PartId);
        }
        else
        {
            mantraInferenceManager.SelectMinorPattern(part.PartId);
        }

        _lastMantraResult = mantraInferenceManager.Infer();
        RefreshMantraVisualUI();
    }

    private void InferMantra()
    {
        if (mantraInferenceManager == null)
        {
            return;
        }

        _lastMantraResult = mantraInferenceManager.Infer();
        UpdateMantraPreview();
    }

    private void ClearMantra()
    {
        if (mantraInferenceManager == null)
        {
            return;
        }

        mantraInferenceManager.ClearSelection();
        _lastMantraResult = MantraInferenceResult.Failed("만트라 조각을 고른 뒤 판정해보자.");
        RefreshMantraVisualUI();
    }

    private void UpdateMantraSelectorLabels()
    {
        if (_mantraMainSelectorText != null)
        {
            _mantraMainSelectorText.text = BuildSelectorText("주 만트라", GetSelectedMantraPartName(MantraPartType.MainImage), MantraPartType.MainImage);
        }

        if (_mantraSupportSelectorText != null)
        {
            _mantraSupportSelectorText.text = BuildSelectorText("보조 만트라", GetSelectedMantraPartName(MantraPartType.SupportImage), MantraPartType.SupportImage);
        }

        if (_mantraMinorSelectorText != null)
        {
            _mantraMinorSelectorText.text = BuildSelectorText("장식 만트라", BuildSelectedMinorPatternNames(), MantraPartType.MinorPattern);
        }
    }

    private void RebuildMantraPartButtons()
    {
        for (int i = _mantraPartContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_mantraPartContent.GetChild(i).gameObject);
        }

        IReadOnlyList<MantraPart> parts = mantraInferenceManager.GetParts(_activeMantraPartType);
        for (int i = 0; i < parts.Count; i++)
        {
            MantraPart part = parts[i];
            CreateMantraPartCard(part);
        }
    }

    private void UpdateMantraPreview()
    {
        if (_mantraPreviewText == null || mantraInferenceManager == null)
        {
            return;
        }

        mantraInferenceManager.TryGetSelectedPart(MantraPartType.MainImage, out MantraPart mainPart);
        mantraInferenceManager.TryGetSelectedPart(MantraPartType.SupportImage, out MantraPart supportPart);
        mantraInferenceManager.TryGetSelectedPart(MantraPartType.MinorPattern, out MantraPart guidePart);

        SetPreviewLayer(_mantraMainPreviewImage, mainPart);
        SetPreviewLayer(_mantraSubPreviewImage, supportPart);
        SetPreviewLayer(_mantraGuidePreviewImage, guidePart);

        _mantraPreviewText.text =
            $"{GetSelectedMantraPartName(MantraPartType.MainImage)} / " +
            $"{GetSelectedMantraPartName(MantraPartType.SupportImage)} / " +
            $"{BuildSelectedMinorPatternNames()}\n";
            //$"<size=16>{mantraInferenceManager.SelectedCombinationKey}</size>";

        if (_mantraResultText != null)
        {
            _mantraResultText.text = $"{_lastMantraResult.Title}\n{_lastMantraResult.Description}";
        }
    }

    private string BuildSelectorText(string title, string value, MantraPartType type)
    {
        string marker = _activeMantraPartType == type ? "▶ " : string.Empty;
        return $"{marker}{title}\n<size=15>{value}</size>";
    }

    private string BuildMantraPartButtonText(MantraPart part)
    {
        string selected = IsMantraPartSelected(part) ? "✓ " : string.Empty;
        return $"{selected}{part.DisplayName}\n<size=13>{part.Description}</size>";
    }

    private void CreateMantraPartCard(MantraPart part)
    {
        GameObject cardObject = new($"MantraPart_{part.PartId}");
        cardObject.transform.SetParent(_mantraPartContent, false);
        RectTransform cardRect = cardObject.AddComponent<RectTransform>();

        Image background = cardObject.AddComponent<Image>();
        background.color = IsMantraPartSelected(part)
            ? new Color(0.48f, 0.42f, 0.28f, 0.95f)
            : new Color(0.88f, 0.83f, 0.68f, 0.92f);

        Button button = cardObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => SelectMantraPart(part));

        RectTransform imageRect = CreateRect("Image", cardRect);
        imageRect.anchorMin = new Vector2(0.12f, 0.38f);
        imageRect.anchorMax = new Vector2(0.88f, 0.92f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        Image icon = imageRect.gameObject.AddComponent<Image>();
        icon.sprite = part.Icon;
        icon.preserveAspect = true;
        icon.color = part.Icon != null ? Color.white : new Color(0.35f, 0.32f, 0.26f, 0.65f);
        icon.raycastTarget = false;

        TMP_Text label = CreateText(cardRect, "Label", leftBodyText != null ? leftBodyText.font : null, 16, TextAlignmentOptions.Center);
        label.text = BuildMantraPartButtonText(part);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.06f, 0.05f);
        labelRect.anchorMax = new Vector2(0.94f, 0.35f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private static Image CreatePreviewLayer(RectTransform parent, string objectName)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = rect.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }

    private static void SetPreviewLayer(Image image, MantraPart part)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = part != null ? part.Icon : null;
        image.color = image.sprite != null ? Color.white : Color.clear;
    }

    private bool IsMantraPartSelected(MantraPart part)
    {
        if (part == null)
        {
            return false;
        }

        return part.Type switch
        {
            MantraPartType.MainImage => part.MatchesId(mantraInferenceManager.SelectedMainImageId),
            MantraPartType.SupportImage => part.MatchesId(mantraInferenceManager.SelectedSupportImageId),
            MantraPartType.MinorPattern => ContainsMantraPartId(mantraInferenceManager.SelectedMinorPatternIds, part.PartId),
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

    private void SetMantraVisualVisible(bool visible)
    {
        if (_mantraLeftRoot != null)
        {
            _mantraLeftRoot.SetActive(visible);
        }

        if (_mantraRightRoot != null)
        {
            _mantraRightRoot.SetActive(visible);
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

    private static TMP_Text CreateMantraButton(RectTransform parent, string objectName, string label, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
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

        if (mantraInferenceManager == null)
        {
            mantraInferenceManager = FindFirstObjectByType<MantraInferenceManager>();
        }

        if (mantraInferenceManager == null)
        {
            mantraInferenceManager = gameObject.AddComponent<MantraInferenceManager>();
        }

        if (dispositionManager == null)
        {
            dispositionManager = FindFirstObjectByType<PlayerDispositionManager>();
        }
    }

    private void BindButtons()
    {
        ResolveMantraTabButton();
        if (assistantTabButton != null)
        {
            assistantTabButton.gameObject.SetActive(showAssistantTab);
        }

        BindButton(evidenceTabButton, ShowEvidenceTab);
        BindButton(npcTabButton, ShowNpcTab);
        BindButton(dialogueLogTabButton, ShowDialogueLogTab);
        BindButton(reflectionTabButton, ShowReflectionTab);
        if (showAssistantTab)
        {
            BindButton(assistantTabButton, ShowAssistantTab);
        }
        BindButton(mantraTabButton, ShowMantraTab);
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
        if (showAssistantTab)
        {
            TmpTextPrewarmUtility.Prewarm(assistantTabButton, TmpPrewarmText);
        }
        TmpTextPrewarmUtility.Prewarm(mantraTabButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(closeButton, TmpPrewarmText);
    }

    private void ResolveMantraTabButton()
    {
        if (mantraTabButton != null)
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
            if (text == "만트라 조사" || text == "만트라 추리" || text == "마법진 조사" || text == "마법진 추리")
            {
                mantraTabButton = button;
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

    private void HandleMantraInput()
    {
        UpdateUiNavigationSuppression();

        if (mantraInferenceManager == null)
        {
            return;
        }

        bool changed = false;
        if (WasMantraKeyPressed(MantraKey.PreviousMain))
        {
            SelectAdjacentMantraPart(MantraPartType.MainImage, -1);
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.NextMain))
        {
            SelectAdjacentMantraPart(MantraPartType.MainImage, 1);
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.PreviousSupport))
        {
            SelectAdjacentMantraPart(MantraPartType.SupportImage, -1);
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.NextSupport))
        {
            SelectAdjacentMantraPart(MantraPartType.SupportImage, 1);
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.PreviousMinor))
        {
            _mantraMinorCursorIndex--;
            ClampMantraCursor(mantraInferenceManager.GetParts(MantraPartType.MinorPattern).Count);
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.NextMinor))
        {
            _mantraMinorCursorIndex++;
            ClampMantraCursor(mantraInferenceManager.GetParts(MantraPartType.MinorPattern).Count);
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.ToggleMinor))
        {
            IReadOnlyList<MantraPart> minorPatterns = mantraInferenceManager.GetParts(MantraPartType.MinorPattern);
            if (minorPatterns.Count > 0)
            {
                ClampMantraCursor(minorPatterns.Count);
                mantraInferenceManager.SelectMinorPattern(minorPatterns[_mantraMinorCursorIndex].PartId);
                changed = true;
            }
        }
        else if (WasMantraKeyPressed(MantraKey.Infer))
        {
            _lastMantraResult = mantraInferenceManager.Infer();
            changed = true;
        }
        else if (WasMantraKeyPressed(MantraKey.Clear))
        {
            mantraInferenceManager.ClearSelection();
            _lastMantraResult = MantraInferenceResult.Failed("만트라 조각을 고른 뒤 판정해보자.");
            changed = true;
        }

        if (changed)
        {
            Refresh();
        }
    }

    private void SelectAdjacentMantraPart(MantraPartType type, int direction)
    {
        IReadOnlyList<MantraPart> parts = mantraInferenceManager.GetParts(type);
        if (parts.Count == 0)
        {
            return;
        }

        string selectedId = type == MantraPartType.MainImage
            ? mantraInferenceManager.SelectedMainImageId
            : mantraInferenceManager.SelectedSupportImageId;
        int selectedIndex = FindMantraPartIndex(parts, selectedId);
        int nextIndex = WrapIndex(selectedIndex + direction, parts.Count);

        if (type == MantraPartType.MainImage)
        {
            mantraInferenceManager.SelectMainImage(parts[nextIndex].PartId);
        }
        else if (type == MantraPartType.SupportImage)
        {
            mantraInferenceManager.SelectSupportImage(parts[nextIndex].PartId);
        }
    }

    private string GetSelectedMantraPartName(MantraPartType type)
    {
        return mantraInferenceManager.TryGetSelectedPart(type, out MantraPart selectedPart)
            ? selectedPart.DisplayName
            : "선택 안 됨";
    }

    private string BuildSelectedMinorPatternNames()
    {
        IReadOnlyList<string> selectedIds = mantraInferenceManager.SelectedMinorPatternIds;
        if (selectedIds.Count == 0)
        {
            return "선택 안 됨";
        }

        IReadOnlyList<MantraPart> minorPatterns = mantraInferenceManager.GetParts(MantraPartType.MinorPattern);
        StringBuilder builder = new();
        for (int i = 0; i < selectedIds.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            int partIndex = FindMantraPartIndex(minorPatterns, selectedIds[i]);
            builder.Append(partIndex >= 0 ? minorPatterns[partIndex].DisplayName : selectedIds[i]);
        }

        return builder.ToString();
    }

    private void AppendMantraPartList(StringBuilder builder, IReadOnlyList<MantraPart> parts, string selectedId, int cursorIndex)
    {
        if (parts.Count == 0)
        {
            builder.AppendLine("- 등록된 조각 없음");
            return;
        }

        IReadOnlyList<string> selectedMinorPatterns = mantraInferenceManager.SelectedMinorPatternIds;
        for (int i = 0; i < parts.Count; i++)
        {
            MantraPart part = parts[i];
            bool selected = !string.IsNullOrWhiteSpace(selectedId) && part.MatchesId(selectedId);
            bool minorSelected = ContainsMantraPartId(selectedMinorPatterns, part.PartId);
            string marker = i == cursorIndex ? ">" : selected || minorSelected ? "*" : "-";
            builder.AppendLine($"{marker} {part.DisplayName}: {part.Description}");
        }
    }

    private void ClampMantraCursor(int count)
    {
        _mantraMinorCursorIndex = count <= 0 ? 0 : WrapIndex(_mantraMinorCursorIndex, count);
    }

    private static int FindMantraPartIndex(IReadOnlyList<MantraPart> parts, string partId)
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

    private static bool ContainsMantraPartId(IReadOnlyList<string> partIds, string partId)
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
            (_currentTab != NotebookTab.Reflection && _currentTab != NotebookTab.Mantra))
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

    private enum MantraKey
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

    private static bool WasMantraTabPressedThisFrame()
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

    private static bool WasMantraKeyPressed(MantraKey key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return key switch
        {
            MantraKey.PreviousMain => keyboard.qKey.wasPressedThisFrame,
            MantraKey.NextMain => keyboard.eKey.wasPressedThisFrame,
            MantraKey.PreviousSupport => keyboard.aKey.wasPressedThisFrame,
            MantraKey.NextSupport => keyboard.dKey.wasPressedThisFrame,
            MantraKey.PreviousMinor => keyboard.zKey.wasPressedThisFrame,
            MantraKey.NextMinor => keyboard.cKey.wasPressedThisFrame,
            MantraKey.ToggleMinor => keyboard.spaceKey.wasPressedThisFrame,
            MantraKey.Infer => keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame,
            MantraKey.Clear => keyboard.rKey.wasPressedThisFrame,
            _ => false
        };
#elif ENABLE_LEGACY_INPUT_MANAGER
        return key switch
        {
            MantraKey.PreviousMain => Input.GetKeyDown(KeyCode.Q),
            MantraKey.NextMain => Input.GetKeyDown(KeyCode.E),
            MantraKey.PreviousSupport => Input.GetKeyDown(KeyCode.A),
            MantraKey.NextSupport => Input.GetKeyDown(KeyCode.D),
            MantraKey.PreviousMinor => Input.GetKeyDown(KeyCode.Z),
            MantraKey.NextMinor => Input.GetKeyDown(KeyCode.C),
            MantraKey.ToggleMinor => Input.GetKeyDown(KeyCode.Space),
            MantraKey.Infer => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter),
            MantraKey.Clear => Input.GetKeyDown(KeyCode.R),
            _ => false
        };
#else
        return false;
#endif
    }
}
