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

    private const string TmpPrewarmText = "수사노트 단서 NPC 대화 기록 단서연결 가설 카드 사색에 잠기기 조수의 정리 기본 관찰 예리한 시야 미세한 청각 침묵의 응시";
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
    [SerializeField] private Button closeButton;

    [Header("Data")]
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private NpcProfileRegistry npcProfileRegistry;
    [SerializeField] private AssistantDiscussionManager assistantDiscussionManager;
    [SerializeField] private PlayerReflectionManager playerReflectionManager;
    [SerializeField] private InvestigationBoardManager investigationBoardManager;
    [SerializeField] private PlayerDispositionManager dispositionManager;

    [Header("Input")]
    [SerializeField] private bool toggleWithKeyboard = true;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.N;

    [Header("Overlay")]
    [SerializeField] private GameObject[] hideWhileOpen;

    private NotebookTab _currentTab = NotebookTab.Evidence;
    private readonly Dictionary<GameObject, bool> _hiddenObjectStates = new();
    private int _firstBoardNodeIndex;
    private int _secondBoardNodeIndex = 1;
    private int _boardScrollIndex;
    private string _lastBoardResult = "연결할 단서 두 개를 고르자.";

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
    }

    public override void Show()
    {
        ResolveReferences();
        HideOverlappedObjects();
        base.Show();
        Refresh();
    }

    public override void Hide()
    {
        base.Hide();
        RestoreHiddenObjects();
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

    private void HandleBoardInput()
    {
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
}
