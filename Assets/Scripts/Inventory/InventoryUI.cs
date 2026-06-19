using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryUI : BasePanelUI
{
    private const string EmptyText = "\uC544\uC9C1 \uAC00\uC9C4 \uBB3C\uAC74\uC774 \uC5C6\uB2E4.";
    private const string NoSelectionText = "\uC67C\uCABD\uC5D0\uC11C \uD655\uC778\uD560 \uBB3C\uAC74\uC744 \uACE0\uB974\uC790.";

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private bool useAsContentTabView = true;
    [SerializeField] private int gridColumns = 4;
    [SerializeField] private int visibleRows = 4;
    [SerializeField] private Vector2 slotSize = new(94f, 94f);
    [SerializeField] private Vector2 slotSpacing = new(20f, 20f);
    [SerializeField] private bool overrideSlotLayout = false;
    [SerializeField] private bool enableUseButton = false;

    [Header("Scene UI References")]
    [SerializeField] private RectTransform itemGridContent;
    [SerializeField] private Image[] itemSlotImages;
    [SerializeField] private Image itemSlotTemplate;
    [SerializeField] private Image selectedItemImage;
    [SerializeField] private TMP_Text selectedItemImageFallbackText;
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailBodyText;
    [SerializeField] private ScrollRect detailBodyScrollRect;
    [SerializeField] private Button inspectButton;
    [SerializeField] private Button combineButton;
    [SerializeField] private Button useButton;

    private RectTransform _itemListContent;
    private GridLayoutGroup _itemGridLayout;
    private TMP_Text _detailTitleText;
    private TMP_Text _detailBodyText;
    private Button _inspectButton;
    private Button _combineButton;
    private Button _useButton;
    private Image _detailIconImage;
    private TMP_Text _detailIconFallbackText;
    private readonly List<GameObject> _itemListEntries = new();
    private readonly List<Button> _itemButtons = new();
    private readonly List<InventorySlotView> _sceneSlots = new();
    private InventoryItem _selectedItem;

    protected override void Awake()
    {
        base.Awake();
        if (!useAsContentTabView)
        {
            EnsureSeparatePanelRoot();
        }

        ResolveReferences();
        BuildIfNeeded();
        Refresh();

        if (!useAsContentTabView)
        {
            Hide();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (inventoryManager != null)
        {
            inventoryManager.ItemAdded += HandleInventoryChanged;
            inventoryManager.ItemInspected += HandleInventoryChanged;
            inventoryManager.ItemRemoved += HandleInventoryChanged;
        }

        if (useAsContentTabView)
        {
            BuildIfNeeded();
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.ItemAdded -= HandleInventoryChanged;
            inventoryManager.ItemInspected -= HandleInventoryChanged;
            inventoryManager.ItemRemoved -= HandleInventoryChanged;
        }
    }

    public override void Show()
    {
        ResolveReferences();
        Refresh();

        if (useAsContentTabView)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            return;
        }

        base.Show();
    }

    public void RefreshView()
    {
        ResolveReferences();
        BuildIfNeeded();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }
    }

    private void BuildIfNeeded()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (TryBindSceneUi())
        {
            return;
        }

        if (useAsContentTabView)
        {
            Debug.LogWarning($"{nameof(InventoryUI)} could not bind the tab inventory hierarchy. Check InventoryView references.", this);
            return;
        }

        if (_itemListContent != null)
        {
            return;
        }

        RectTransform root = panelRoot.GetComponent<RectTransform>();
        if (root == null)
        {
            root = panelRoot.AddComponent<RectTransform>();
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image dim = panelRoot.GetComponent<Image>();
        if (dim == null)
        {
            dim = panelRoot.AddComponent<Image>();
        }

        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        GameObject window = CreateUiObject("InventoryWindow", panelRoot.transform);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(1040f, 620f);
        windowRect.anchoredPosition = Vector2.zero;
        Image windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.88f, 0.83f, 0.70f, 0.96f);

        TMP_Text title = CreateText("TitleText", window.transform, "\uC778\uBCA4\uD1A0\uB9AC", 32, TextAlignmentOptions.Left);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(-150f, -76f));

        Button closeButton = CreateButton("CloseButton", window.transform, "\uB2EB\uAE30", 18);
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-128f, -24f), new Vector2(-28f, -66f));
        closeButton.onClick.AddListener(Hide);

        GameObject detailPanel = CreatePanel("ItemDetailPanel", window.transform, new Color(0.95f, 0.90f, 0.76f, 0.92f));
        SetRect(detailPanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, 34f), new Vector2(424f, -92f));

        GameObject gridPanel = CreatePanel("ItemGridPanel", window.transform, new Color(0.95f, 0.90f, 0.76f, 0.92f));
        SetRect(gridPanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(460f, 34f), new Vector2(-34f, -92f));

        GameObject iconPanel = CreatePanel("SelectedItemImagePanel", detailPanel.transform, new Color(0.10f, 0.39f, 0.49f, 1f));
        SetRect(iconPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -226f), new Vector2(-24f, -24f));

        _detailIconImage = CreateUiObject("SelectedItemImage", iconPanel.transform).AddComponent<Image>();
        _detailIconImage.preserveAspect = true;
        _detailIconImage.raycastTarget = false;
        SetRect(_detailIconImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 12f), new Vector2(-12f, -12f));

        _detailIconFallbackText = CreateText("SelectedItemImageFallbackText", iconPanel.transform, "\uC774\uBBF8\uC9C0", 30, TextAlignmentOptions.Center);
        _detailIconFallbackText.color = Color.white;
        SetRect(_detailIconFallbackText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-8f, -8f));

        GameObject descriptionPanel = CreatePanel("SelectedItemDescriptionPanel", detailPanel.transform, new Color(0.10f, 0.39f, 0.49f, 1f));
        SetRect(descriptionPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 92f), new Vector2(-24f, -246f));

        ScrollRect scrollRect = gridPanel.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreatePanel("Viewport", gridPanel.transform, new Color(1f, 1f, 1f, 0f));
        SetRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(28f, 28f), new Vector2(-28f, -28f));
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateUiObject("Content", viewport.transform);
        _itemListContent = content.GetComponent<RectTransform>();
        _itemListContent.anchorMin = new Vector2(0f, 1f);
        _itemListContent.anchorMax = new Vector2(1f, 1f);
        _itemListContent.pivot = new Vector2(0.5f, 1f);
        _itemListContent.anchoredPosition = Vector2.zero;
        _itemListContent.sizeDelta = new Vector2(0f, 0f);

        _itemGridLayout = content.AddComponent<GridLayoutGroup>();
        _itemGridLayout.cellSize = slotSize;
        _itemGridLayout.spacing = slotSpacing;
        _itemGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _itemGridLayout.constraintCount = Mathf.Max(1, gridColumns);
        _itemGridLayout.childAlignment = TextAnchor.UpperCenter;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = _itemListContent;

        _detailTitleText = CreateText("DetailTitleText", descriptionPanel.transform, "\uBB3C\uAC74 \uC815\uBCF4", 24, TextAlignmentOptions.Center);
        _detailTitleText.color = Color.white;
        SetRect(_detailTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -58f), new Vector2(-16f, -16f));

        _detailBodyText = CreateText("DetailBodyText", descriptionPanel.transform, NoSelectionText, 20, TextAlignmentOptions.TopLeft);
        _detailBodyText.color = Color.white;
        SetRect(_detailBodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -68f));

        _inspectButton = CreateButton("InspectButton", detailPanel.transform, "\uC870\uC0AC\uD558\uAE30", 20);
        SetRect(_inspectButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(138f, 68f));
        _inspectButton.onClick.AddListener(InspectSelectedItem);

        _combineButton = CreateButton("CombineButton", detailPanel.transform, "\uD569\uCE58\uAE30", 20);
        SetRect(_combineButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-57f, 24f), new Vector2(57f, 68f));
        _combineButton.interactable = false;

        _useButton = CreateButton("UseButton", detailPanel.transform, "\uC0AC\uC6A9\uD558\uAE30", 20);
        SetRect(_useButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-138f, 24f), new Vector2(-24f, 68f));
        _useButton.onClick.AddListener(UseSelectedItem);
        _useButton.interactable = false;
    }

    private bool TryBindSceneUi()
    {
        Transform root = panelRoot != null ? panelRoot.transform : transform;
        TryBindKnownInventoryHierarchy(root);
        TryBindTabInventoryHierarchy(root);

        if (itemGridContent == null)
        {
            itemGridContent = FindChildByName(root, "Content") as RectTransform;
        }

        if (selectedItemImage == null)
        {
            selectedItemImage = FindChildComponent<Image>(root, "SelectedItemImage");
        }

        if (selectedItemImageFallbackText == null)
        {
            selectedItemImageFallbackText = FindChildComponent<TMP_Text>(root, "SelectedItemImageFallbackText");
        }

        if (detailTitleText == null)
        {
            detailTitleText = FindChildComponent<TMP_Text>(root, "DetailTitleText");
        }

        if (detailBodyText == null)
        {
            detailBodyText = FindChildComponent<TMP_Text>(root, "DetailBodyText");
        }

        if (detailBodyScrollRect == null)
        {
            detailBodyScrollRect = FindChildComponent<ScrollRect>(root, "DescriptionScrollView");
        }

        if (inspectButton == null)
        {
            inspectButton = FindChildComponent<Button>(root, "InspectButton");
        }

        if (combineButton == null)
        {
            combineButton = FindChildComponent<Button>(root, "CombineButton");
        }

        if (useButton == null)
        {
            useButton = FindChildComponent<Button>(root, "UseButton");
        }

        if (itemGridContent == null || selectedItemImage == null || detailTitleText == null || detailBodyText == null)
        {
            return false;
        }

        _itemListContent = itemGridContent;
        _itemGridLayout = _itemListContent.GetComponent<GridLayoutGroup>();
        if (_itemGridLayout == null && ShouldOverrideSlotLayout)
        {
            _itemGridLayout = _itemListContent.gameObject.AddComponent<GridLayoutGroup>();
        }

        if (_itemGridLayout != null && ShouldOverrideSlotLayout)
        {
            _itemGridLayout.cellSize = slotSize;
            _itemGridLayout.spacing = slotSpacing;
            _itemGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _itemGridLayout.constraintCount = Mathf.Max(1, gridColumns);
            _itemGridLayout.childAlignment = TextAnchor.UpperCenter;
        }

        if (itemSlotTemplate == null)
        {
            itemSlotTemplate = FindFirstDirectChildImage(_itemListContent);
        }

        BindSceneSlots();

        if (_sceneSlots.Count == 0 && itemSlotTemplate != null)
        {
            RectTransform templateRect = itemSlotTemplate.rectTransform;
            if (ShouldOverrideSlotLayout)
            {
                templateRect.anchorMin = new Vector2(0f, 1f);
                templateRect.anchorMax = new Vector2(0f, 1f);
                templateRect.sizeDelta = slotSize;
            }
            itemSlotTemplate.gameObject.SetActive(false);
        }

        _detailIconImage = selectedItemImage;
        _detailIconFallbackText = selectedItemImageFallbackText;
        _detailTitleText = detailTitleText;
        _detailBodyText = detailBodyText;
        _inspectButton = inspectButton;
        _combineButton = combineButton;
        _useButton = useButton;

        if (_inspectButton != null)
        {
            _inspectButton.onClick.RemoveListener(InspectSelectedItem);
            _inspectButton.onClick.AddListener(InspectSelectedItem);
        }

        if (_combineButton != null)
        {
            _combineButton.interactable = false;
        }

        if (_useButton != null)
        {
            _useButton.onClick.RemoveListener(UseSelectedItem);
            _useButton.onClick.AddListener(UseSelectedItem);
            _useButton.interactable = false;
        }

        return true;
    }

    private void TryBindTabInventoryHierarchy(Transform root)
    {
        Transform inventoryView = root != null && root.name == "InventoryView"
            ? root
            : FindChildByName(root, "InventoryView");

        if (inventoryView == null)
        {
            return;
        }

        if (panelRoot == null || panelRoot.name == "InventoryPanelRoot")
        {
            panelRoot = inventoryView.gameObject;
        }

        Transform topWindow = FindChildByName(inventoryView, "Top_InventoryWindow");
        Transform botSlot = FindChildByName(inventoryView, "Bot_Slot");

        if (botSlot != null && itemGridContent == null)
        {
            Transform viewport = FindChildByName(botSlot, "Viewport");
            Transform content = FindChildByName(viewport, "Content");
            itemGridContent = content as RectTransform;
        }

        if (topWindow == null)
        {
            return;
        }

        Transform selectedImagePanel = FindChildByName(topWindow, "SelectedItemImagePanel");
        Transform descriptionPanel = FindChildByName(topWindow, "SelectedItemDescriptionPanel");

        if (selectedImagePanel != null)
        {
            selectedItemImage ??= FindChildComponent<Image>(selectedImagePanel, "SelectedItemImage");
            selectedItemImageFallbackText ??= FindChildComponent<TMP_Text>(selectedImagePanel, "SelectedItemImageFallbackText");
        }

        if (descriptionPanel != null)
        {
            detailTitleText ??= FindChildComponent<TMP_Text>(descriptionPanel, "DetailTitleText");
            detailBodyScrollRect ??= FindChildComponent<ScrollRect>(descriptionPanel, "DescriptionScrollView");
            detailBodyText ??= FindChildComponent<TMP_Text>(descriptionPanel, "DetailBodyText");
        }

        inspectButton ??= FindChildComponent<Button>(topWindow, "InspectButton");
        combineButton ??= FindChildComponent<Button>(topWindow, "CombineButton");
        useButton ??= FindChildComponent<Button>(topWindow, "UseButton");
    }

    private void TryBindKnownInventoryHierarchy(Transform root)
    {
        Transform window = GetNamedChild(root, 0, "InventoryWindow");
        if (window == null)
        {
            return;
        }

        Transform detailPanel = GetNamedChild(window, 2, "ItemDetailPanel");
        Transform gridPanel = GetNamedChild(window, 3, "ItemGridPanel");

        if (gridPanel != null && itemGridContent == null)
        {
            Transform viewport = GetNamedChild(gridPanel, 0, "Viewport");
            Transform content = GetNamedChild(viewport, 0, "Content");
            itemGridContent = content as RectTransform;
        }

        if (detailPanel == null)
        {
            return;
        }

        Transform selectedImagePanel = GetNamedChild(detailPanel, 0, "SelectedItemImagePanel");
        Transform descriptionPanel = GetNamedChild(detailPanel, 1, "SelectedItemDescriptionPanel");

        if (selectedImagePanel != null)
        {
            selectedItemImage ??= GetComponentFromNamedChild<Image>(selectedImagePanel, 0, "SelectedItemImage");
            selectedItemImageFallbackText ??= GetComponentFromNamedChild<TMP_Text>(selectedImagePanel, 1, "SelectedItemImageFallbackText");
        }

        if (descriptionPanel != null)
        {
            detailTitleText ??= GetComponentFromNamedChild<TMP_Text>(descriptionPanel, 0, "DetailTitleText");
            detailBodyText ??= GetComponentFromNamedChild<TMP_Text>(descriptionPanel, 1, "DetailBodyText");
        }

        inspectButton ??= GetComponentFromNamedChild<Button>(detailPanel, 2, "InspectButton");
        combineButton ??= GetComponentFromNamedChild<Button>(detailPanel, 3, "CombineButton");
        useButton ??= GetComponentFromNamedChild<Button>(detailPanel, 4, "UseButton");
    }

    private void BindSceneSlots()
    {
        _sceneSlots.Clear();

        if (itemSlotImages == null || itemSlotImages.Length == 0)
        {
            itemSlotImages = FindDirectChildImages(_itemListContent);
        }

        if (itemSlotImages == null)
        {
            return;
        }

        foreach (Image slotImage in itemSlotImages)
        {
            if (slotImage == null)
            {
                continue;
            }

            RectTransform rect = slotImage.rectTransform;
            if (rect != null && ShouldOverrideSlotLayout)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.sizeDelta = slotSize;
            }

            Button button = slotImage.GetComponent<Button>();
            if (button == null)
            {
                button = slotImage.gameObject.AddComponent<Button>();
            }
            ConfigureSlotButtonColors(button, slotImage.color);

            InventorySlotView slot = new(slotImage, button);
            int index = _sceneSlots.Count;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectSceneSlot(index));
            _sceneSlots.Add(slot);
        }
    }

    private void SelectSceneSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _sceneSlots.Count)
        {
            return;
        }

        InventoryItem item = _sceneSlots[slotIndex].Item;
        if (item == null)
        {
            return;
        }

        _selectedItem = item;
        RefreshDetail();
    }

    private void EnsureSeparatePanelRoot()
    {
        if (panelRoot != gameObject)
        {
            return;
        }

        GameObject rootObject = new("InventoryPanelRoot", typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panelRoot = rootObject;
    }

    private void Refresh()
    {
        if (_itemListContent == null)
        {
            return;
        }

        if (_sceneSlots.Count > 0)
        {
            RefreshSceneSlots();
            return;
        }

        foreach (GameObject entry in _itemListEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }

        _itemListEntries.Clear();
        _itemButtons.Clear();

        IReadOnlyCollection<InventoryItem> items = inventoryManager != null ? inventoryManager.Items : null;
        int minimumSlotCount = Mathf.Max(1, gridColumns) * Mathf.Max(1, visibleRows);
        if (items == null || items.Count == 0)
        {
            for (int i = 0; i < minimumSlotCount; i++)
            {
                GameObject emptySlot = CreateEmptySlot(i == 0 ? EmptyText : string.Empty);
                _itemListEntries.Add(emptySlot);
            }

            UpdateGridContentHeight(minimumSlotCount);
            _selectedItem = null;
            RefreshDetail();
            return;
        }

        bool selectedStillExists = false;
        foreach (InventoryItem item in items)
        {
            if (_selectedItem != null && item.MatchesId(_selectedItem.ItemId))
            {
                selectedStillExists = true;
            }
        }

        if (!selectedStillExists)
        {
            _selectedItem = null;
        }

        int itemCount = 0;
        foreach (InventoryItem item in items)
        {
            if (_selectedItem == null)
            {
                _selectedItem = item;
            }

            Button button = CreateItemSlotButton(item);
            InventoryItem capturedItem = item;
            button.onClick.AddListener(() =>
            {
                _selectedItem = capturedItem;
                RefreshDetail();
            });
            _itemListEntries.Add(button.gameObject);
            _itemButtons.Add(button);
            itemCount++;
        }

        for (int i = itemCount; i < minimumSlotCount; i++)
        {
            GameObject emptySlot = CreateEmptySlot(string.Empty);
            _itemListEntries.Add(emptySlot);
        }

        UpdateGridContentHeight(Mathf.Max(itemCount, minimumSlotCount));
        RefreshDetail();
    }

    private void RefreshSceneSlots()
    {
        IReadOnlyCollection<InventoryItem> items = inventoryManager != null ? inventoryManager.Items : null;
        int slotIndex = 0;
        bool selectedStillExists = false;

        if (items != null)
        {
            foreach (InventoryItem item in items)
            {
                if (_selectedItem != null && item.MatchesId(_selectedItem.ItemId))
                {
                    selectedStillExists = true;
                }
            }

            if (!selectedStillExists)
            {
                _selectedItem = null;
            }

            foreach (InventoryItem item in items)
            {
                if (slotIndex >= _sceneSlots.Count)
                {
                    break;
                }

                if (_selectedItem == null)
                {
                    _selectedItem = item;
                }

                _sceneSlots[slotIndex].SetItem(item);
                slotIndex++;
            }
        }

        for (int i = slotIndex; i < _sceneSlots.Count; i++)
        {
            _sceneSlots[i].Clear();
        }

        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_selectedItem == null)
        {
            if (_detailTitleText != null)
            {
                _detailTitleText.text = "\uBB3C\uAC74 \uC815\uBCF4";
            }

            if (_detailBodyText != null)
            {
                _detailBodyText.text = NoSelectionText;
            }

            ResetDetailScrollToTop();
            SetDetailIcon(null, string.Empty);

            if (_inspectButton != null)
            {
                _inspectButton.interactable = false;
            }

            if (_combineButton != null)
            {
                _combineButton.interactable = false;
            }

            if (_useButton != null)
            {
                _useButton.interactable = false;
            }

            return;
        }

        if (_detailTitleText != null)
        {
            _detailTitleText.text = _selectedItem.DisplayName;
        }

        SetDetailIcon(_selectedItem.Icon, _selectedItem.DisplayName);

        if (_detailBodyText != null)
        {
            StringBuilder builder = new();
            builder.AppendLine(_selectedItem.Description);

            if (_selectedItem.Inspected)
            {
                _detailBodyText.text = string.IsNullOrWhiteSpace(_selectedItem.InspectResultText)
                    ? "특별히 더 알아낼 만한 것은 없다."
                    : _selectedItem.InspectResultText;
            }
            else
            {
                _detailBodyText.text = _selectedItem.Description;
            }

            ResetDetailScrollToTop();
        }

        if (_inspectButton != null)
        {
            _inspectButton.interactable = !_selectedItem.Inspected;
        }

        if (_combineButton != null)
        {
            _combineButton.interactable = false;
        }

        if (_useButton != null)
        {
            _useButton.interactable = enableUseButton;
        }
    }

    private void InspectSelectedItem()
    {
        if (_selectedItem == null || inventoryManager == null)
        {
            return;
        }

        bool newlyInspected = inventoryManager.MarkInspected(_selectedItem.ItemId);
        if (!newlyInspected)
        {
            RefreshDetail();
            return;
        }

        bool rewarded = false;
        if (evidenceInventory != null)
        {
            if (!string.IsNullOrWhiteSpace(_selectedItem.RewardEvidenceId))
            {
                rewarded |= evidenceInventory.AddEvidence(_selectedItem.RewardEvidenceId);
            }

            if (!string.IsNullOrWhiteSpace(_selectedItem.RewardKeywordId))
            {
                rewarded |= evidenceInventory.AddKeyword(_selectedItem.RewardKeywordId);
            }
        }

        UIManager.Instance?.ShowNotification(
            "\uBB3C\uAC74 \uC870\uC0AC",
            rewarded ? "\uC218\uC0AC\uB178\uD2B8\uC5D0 \uC0C8 \uC815\uBCF4\uAC00 \uCD94\uAC00\uB418\uC5C8\uB2E4." : "\uBB3C\uAC74\uC744 \uC790\uC138\uD788 \uC0B4\uD3B4\uBCF4\uC558\uB2E4.");

        Refresh();
    }

    private void UseSelectedItem()
    {
        if (_selectedItem == null || inventoryManager == null)
        {
            return;
        }

        string usedItemName = _selectedItem.DisplayName;
        string usedItemId = _selectedItem.ItemId;
        if (!inventoryManager.RemoveItem(usedItemId))
        {
            Refresh();
            return;
        }

        _selectedItem = null;
        UIManager.Instance?.ShowNotification("\uBB3C\uAC74 \uC0AC\uC6A9", $"{usedItemName}\uC744(\uB97C) \uC0AC\uC6A9\uD588\uB2E4.");
        Refresh();
    }

    private void HandleInventoryChanged(InventoryItem item)
    {
        if (IsVisible)
        {
            Refresh();
        }
    }

    private Button CreateItemSlotButton(InventoryItem item)
    {
        GameObject slotObject = CreateSlotObject($"ItemSlot_{item.ItemId}", new Color(0.08f, 0.32f, 0.42f, 1f));
        Button button = slotObject.GetComponent<Button>();
        if (button == null)
        {
            button = slotObject.AddComponent<Button>();
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.32f, 0.42f, 1f);
        colors.highlightedColor = new Color(0.15f, 0.50f, 0.62f, 1f);
        colors.pressedColor = new Color(0.07f, 0.28f, 0.36f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateItemIconVisual(slotObject.transform, item.Icon, item.DisplayName, 14);

        if (item.Inspected)
        {
            TMP_Text inspectedMark = CreateText("InspectedMark", slotObject.transform, "\u2713", 20, TextAlignmentOptions.TopRight);
            inspectedMark.color = Color.white;
            SetRect(inspectedMark.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(6f, 4f), new Vector2(-6f, -4f));
        }

        return button;
    }

    private GameObject CreateEmptySlot(string label)
    {
        GameObject slotObject = CreateSlotObject("EmptySlot", new Color(0.08f, 0.32f, 0.42f, 0.65f));
        Image image = slotObject.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            TMP_Text labelText = CreateText("EmptySlotLabel", slotObject.transform, label, 15, TextAlignmentOptions.Center);
            labelText.color = new Color(1f, 1f, 1f, 0.75f);
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(6f, 6f), new Vector2(-6f, -6f));
        }

        return slotObject;
    }

    private GameObject CreateSlotObject(string objectName, Color color)
    {
        GameObject slotObject;
        if (itemSlotTemplate != null)
        {
            slotObject = Instantiate(itemSlotTemplate.gameObject, _itemListContent);
            slotObject.name = objectName;
            slotObject.SetActive(true);

            foreach (Transform child in slotObject.transform)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            slotObject = CreatePanel(objectName, _itemListContent, color);
        }

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        if (rect != null && ShouldOverrideSlotLayout)
        {
            rect.sizeDelta = slotSize;
        }

        Image image = slotObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        return slotObject;
    }

    private void CreateItemIconVisual(Transform parent, Sprite icon, string label, int fontSize)
    {
        Image iconImage = CreateUiObject("Icon", parent).AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        SetRect(iconImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-8f, -8f));

        if (icon != null)
        {
            return;
        }

        GameObject tempIcon = CreatePanel("TemporaryIcon", parent, new Color(0.12f, 0.43f, 0.55f, 1f));
        SetRect(tempIcon.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-8f, -8f));

        GameObject inner = CreatePanel("TemporaryIconInner", tempIcon.transform, new Color(0.16f, 0.54f, 0.67f, 1f));
        SetRect(inner.GetComponent<RectTransform>(), new Vector2(0.18f, 0.46f), new Vector2(0.82f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text fallbackText = CreateText("FallbackText", tempIcon.transform, ShortenLabel(label), fontSize, TextAlignmentOptions.Center);
        fallbackText.color = Color.white;
        fallbackText.fontStyle = FontStyles.Bold;
        SetRect(fallbackText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(6f, 6f), new Vector2(-6f, -6f));
    }

    private void UpdateGridContentHeight(int slotCount)
    {
        if (_itemListContent == null)
        {
            return;
        }

        int columns = GetEffectiveColumnCount();
        int rows = Mathf.Max(1, Mathf.CeilToInt(slotCount / (float)columns));
        Vector2 effectiveSlotSize = GetEffectiveSlotSize();
        Vector2 effectiveSlotSpacing = GetEffectiveSlotSpacing();
        float height = rows * effectiveSlotSize.y + Mathf.Max(0, rows - 1) * effectiveSlotSpacing.y;
        _itemListContent.sizeDelta = new Vector2(0f, height);

        if (_itemGridLayout != null && ShouldOverrideSlotLayout)
        {
            _itemGridLayout.cellSize = slotSize;
            _itemGridLayout.spacing = slotSpacing;
            _itemGridLayout.constraintCount = columns;
        }
    }

    private bool ShouldOverrideSlotLayout => !useAsContentTabView || overrideSlotLayout;

    private int GetEffectiveColumnCount()
    {
        if (_itemGridLayout != null && !ShouldOverrideSlotLayout)
        {
            return _itemGridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, _itemGridLayout.constraintCount)
                : Mathf.Max(1, gridColumns);
        }

        return Mathf.Max(1, gridColumns);
    }

    private Vector2 GetEffectiveSlotSize()
    {
        if (_itemGridLayout != null && !ShouldOverrideSlotLayout)
        {
            return _itemGridLayout.cellSize;
        }

        return slotSize;
    }

    private Vector2 GetEffectiveSlotSpacing()
    {
        if (_itemGridLayout != null && !ShouldOverrideSlotLayout)
        {
            return _itemGridLayout.spacing;
        }

        return slotSpacing;
    }

    private static string ShortenLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "?";
        }

        string trimmed = label.Trim();
        return trimmed.Length <= 6 ? trimmed : trimmed[..6];
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChildByName(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Image FindFirstDirectChildImage(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.TryGetComponent(out Image image))
            {
                return image;
            }
        }

        return null;
    }

    private static Image[] FindDirectChildImages(Transform root)
    {
        if (root == null)
        {
            return System.Array.Empty<Image>();
        }

        List<Image> images = new();
        foreach (Transform child in root)
        {
            if (child.TryGetComponent(out Image image))
            {
                images.Add(image);
            }
        }

        return images.ToArray();
    }

    private static Transform GetNamedChild(Transform parent, int index, string expectedName)
    {
        if (parent == null || index < 0 || index >= parent.childCount)
        {
            return null;
        }

        Transform child = parent.GetChild(index);
        return child != null && child.name == expectedName ? child : null;
    }

    private static T GetComponentFromNamedChild<T>(Transform parent, int index, string expectedName) where T : Component
    {
        Transform child = GetNamedChild(parent, index, expectedName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private void SetDetailIcon(Sprite sprite, string fallbackLabel)
    {
        if (_detailIconImage != null)
        {
            _detailIconImage.sprite = sprite;
            _detailIconImage.enabled = sprite != null;
        }

        if (_detailIconFallbackText != null)
        {
            _detailIconFallbackText.text = string.IsNullOrWhiteSpace(fallbackLabel) ? "\uC774\uBBF8\uC9C0" : fallbackLabel;
            _detailIconFallbackText.gameObject.SetActive(sprite == null);
        }
    }

    private void ResetDetailScrollToTop()
    {
        if (detailBodyScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (detailBodyScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailBodyScrollRect.content);
        }

        detailBodyScrollRect.StopMovement();
        detailBodyScrollRect.verticalNormalizedPosition = 1f;
        detailBodyScrollRect.velocity = Vector2.zero;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = CreateUiObject(objectName, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return panel;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TMP_Text tmpText = textObject.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.color = new Color(0.18f, 0.16f, 0.13f, 1f);
        tmpText.alignment = alignment;
        tmpText.textWrappingMode = TextWrappingModes.Normal;
        tmpText.raycastTarget = false;
        return tmpText;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, int fontSize)
    {
        GameObject buttonObject = CreatePanel(objectName, parent, new Color(0.34f, 0.30f, 0.25f, 0.95f));
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.34f, 0.30f, 0.25f, 0.95f);
        colors.highlightedColor = new Color(0.44f, 0.39f, 0.32f, 1f);
        colors.pressedColor = new Color(0.24f, 0.21f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TMP_Text labelText = CreateText("Label", buttonObject.transform, label, fontSize, TextAlignmentOptions.Center);
        labelText.color = Color.white;
        SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 4f), new Vector2(-8f, -4f));
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void ConfigureSlotButtonColors(Button button, Color baseColor)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = baseColor.a > 0f ? baseColor : Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private sealed class InventorySlotView
    {
        private readonly Image _slotImage;
        private readonly Button _button;
        private readonly Sprite _emptySprite;
        private readonly Color _emptyColor;

        public InventorySlotView(Image slotImage, Button button)
        {
            _slotImage = slotImage;
            _button = button;
            _emptySprite = slotImage != null ? slotImage.sprite : null;
            _emptyColor = slotImage != null ? slotImage.color : Color.white;
            Clear();
        }

        public InventoryItem Item { get; private set; }

        public void SetItem(InventoryItem item)
        {
            Item = item;

            if (_slotImage != null)
            {
                _slotImage.sprite = item != null && item.Icon != null ? item.Icon : _emptySprite;
                _slotImage.color = item != null && item.Icon != null ? Color.white : _emptyColor;
                _slotImage.enabled = true;
            }

            if (_button != null)
            {
                _button.interactable = true;
            }
        }

        public void Clear()
        {
            Item = null;

            if (_slotImage != null)
            {
                _slotImage.sprite = _emptySprite;
                _slotImage.color = _emptyColor;
                _slotImage.enabled = true;
            }

            if (_button != null)
            {
                _button.interactable = true;
            }
        }
    }
}
