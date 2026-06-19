using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MantraViewController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MantraInferenceManager mantraInferenceManager;

    [Header("Category Buttons")]
    [SerializeField] private Button mainMantraButton;
    [SerializeField] private Button subMantraButton;
    [SerializeField] private Button guideMantraButton;

    [Header("Preview")]
    [SerializeField] private Image mainPreviewImage;
    [SerializeField] private Image subPreviewImage;
    [SerializeField] private Image guidePreviewImage;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;

    [Header("Selected Part Texts")]
    [SerializeField] private TMP_Text mainMantraText;
    [SerializeField] private TMP_Text subMantraText;
    [SerializeField] private TMP_Text guideMantraText;

    [Header("Reusable Part Slots")]
    [SerializeField] private Transform partSlotRoot;
    [SerializeField] private bool addMissingButtonComponents = true;
    [SerializeField] private bool hideUnusedSlots = true;

    [Header("Colors")]
    [SerializeField] private Color activeCategoryColor = new(0.95f, 0.95f, 1f, 1f);
    [SerializeField] private Color normalCategoryColor = Color.white;
    [SerializeField] private bool tintSlotBackground = true;
    [SerializeField] private Color selectedSlotColor = new(1f, 0.92f, 0.55f, 1f);
    [SerializeField] private Color normalSlotColor = Color.white;
    [SerializeField] private Color emptySlotColor = new(1f, 1f, 1f, 0.25f);

    private readonly List<PartSlot> _partSlots = new();
    private MantraPartType _activeType = MantraPartType.MainImage;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        RebuildSlots();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        RebuildSlots();

        if (mantraInferenceManager != null)
        {
            mantraInferenceManager.Changed += RefreshView;
        }

        ShowMainParts();
    }

    private void OnDisable()
    {
        if (mantraInferenceManager != null)
        {
            mantraInferenceManager.Changed -= RefreshView;
        }
    }

    public void ShowMainParts()
    {
        ShowParts(MantraPartType.MainImage);
    }

    public void ShowSubParts()
    {
        ShowParts(MantraPartType.SupportImage);
    }

    public void ShowGuideParts()
    {
        ShowParts(MantraPartType.MinorPattern);
    }

    public void Infer()
    {
        if (mantraInferenceManager == null)
        {
            return;
        }

        MantraInferenceResult result = mantraInferenceManager.Infer();
        SetResultText(result.Title, result.Description);
        RefreshView();
    }

    public void ClearSelection()
    {
        if (mantraInferenceManager == null)
        {
            return;
        }

        mantraInferenceManager.ClearSelection();
        SetResultText(string.Empty, string.Empty);
        RefreshView();
    }

    private void ShowParts(MantraPartType type)
    {
        _activeType = type;
        RefreshView();
    }

    private void RefreshView()
    {
        ResolveReferences();
        RebuildSlots();
        RefreshCategoryButtons();
        RefreshPreview();
        RefreshSelectedPartTexts();
        RefreshPartSlots();
    }

    private void RefreshCategoryButtons()
    {
        SetButtonColor(mainMantraButton, _activeType == MantraPartType.MainImage);
        SetButtonColor(subMantraButton, _activeType == MantraPartType.SupportImage);
        SetButtonColor(guideMantraButton, _activeType == MantraPartType.MinorPattern);
    }

    private void RefreshPreview()
    {
        SetPreview(mainPreviewImage, MantraPartType.MainImage);
        SetPreview(subPreviewImage, MantraPartType.SupportImage);
        SetPreview(guidePreviewImage, MantraPartType.MinorPattern);
    }

    private void RefreshPartSlots()
    {
        if (mantraInferenceManager == null)
        {
            return;
        }

        IReadOnlyList<MantraPart> parts = mantraInferenceManager.GetParts(_activeType);
        for (int i = 0; i < _partSlots.Count; i++)
        {
            MantraPart part = i < parts.Count ? parts[i] : null;
            _partSlots[i].SetPart(part, IsSelected(part), hideUnusedSlots);

            Button button = _partSlots[i].Button;
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = part != null;
            if (part != null)
            {
                MantraPart capturedPart = part;
                button.onClick.AddListener(() => SelectPart(capturedPart));
            }
        }

        if (parts.Count > _partSlots.Count)
        {
            Debug.LogWarning($"{nameof(MantraViewController)} has {parts.Count} {_activeType} parts, but only {_partSlots.Count} reusable slots.", this);
        }
    }

    private void SelectPart(MantraPart part)
    {
        if (part == null || mantraInferenceManager == null)
        {
            return;
        }

        switch (part.Type)
        {
            case MantraPartType.MainImage:
                mantraInferenceManager.SelectMainImage(
                    part.MatchesId(mantraInferenceManager.SelectedMainImageId) ? string.Empty : part.PartId);
                break;
            case MantraPartType.SupportImage:
                mantraInferenceManager.SelectSupportImage(
                    part.MatchesId(mantraInferenceManager.SelectedSupportImageId) ? string.Empty : part.PartId);
                break;
            case MantraPartType.MinorPattern:
                mantraInferenceManager.SelectMinorPattern(
                    part.MatchesId(mantraInferenceManager.SelectedMinorPatternId) ? string.Empty : part.PartId);
                break;
        }

        SetResultText(string.Empty, string.Empty);
    }

    private bool IsSelected(MantraPart part)
    {
        if (part == null || mantraInferenceManager == null)
        {
            return false;
        }

        return part.Type switch
        {
            MantraPartType.MainImage => part.MatchesId(mantraInferenceManager.SelectedMainImageId),
            MantraPartType.SupportImage => part.MatchesId(mantraInferenceManager.SelectedSupportImageId),
            MantraPartType.MinorPattern => part.MatchesId(mantraInferenceManager.SelectedMinorPatternId),
            _ => false
        };
    }

    private void SetPreview(Image target, MantraPartType type)
    {
        if (target == null || mantraInferenceManager == null)
        {
            return;
        }

        bool hasPart = mantraInferenceManager.TryGetSelectedPart(type, out MantraPart part);
        target.sprite = hasPart ? part.Icon : null;
        target.color = hasPart && part.Icon != null ? Color.white : emptySlotColor;
        target.enabled = true;
        target.preserveAspect = true;
    }

    private void SetResultText(string title, string body)
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = title ?? string.Empty;
        }

        if (resultBodyText != null)
        {
            resultBodyText.text = body ?? string.Empty;
        }
    }

    private void RefreshSelectedPartTexts()
    {
        SetSelectedPartText(mainMantraText, MantraPartType.MainImage);
        SetSelectedPartText(subMantraText, MantraPartType.SupportImage);
        SetSelectedPartText(guideMantraText, MantraPartType.MinorPattern);
    }

    private void SetSelectedPartText(TMP_Text target, MantraPartType type)
    {
        if (target == null)
        {
            return;
        }

        if (mantraInferenceManager == null ||
            !mantraInferenceManager.TryGetSelectedPart(type, out MantraPart part))
        {
            target.text = string.Empty;
            return;
        }

        target.text = string.IsNullOrWhiteSpace(part.Description)
            ? part.DisplayName
            : $"{part.DisplayName}\n{part.Description}";
    }

    private void BindButtons()
    {
        BindButton(mainMantraButton, ShowMainParts);
        BindButton(subMantraButton, ShowSubParts);
        BindButton(guideMantraButton, ShowGuideParts);
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

    private void RebuildSlots()
    {
        _partSlots.Clear();
        if (partSlotRoot == null)
        {
            return;
        }

        foreach (Transform child in partSlotRoot)
        {
            if (!child.gameObject.activeSelf && !hideUnusedSlots)
            {
                continue;
            }

            _partSlots.Add(new PartSlot(
                child.gameObject,
                addMissingButtonComponents,
                tintSlotBackground,
                normalSlotColor,
                selectedSlotColor,
                emptySlotColor));
        }
    }

    private void ResolveReferences()
    {
        if (mantraInferenceManager == null)
        {
            mantraInferenceManager = FindFirstObjectByType<MantraInferenceManager>();
        }

        if (mantraInferenceManager == null)
        {
            mantraInferenceManager = gameObject.AddComponent<MantraInferenceManager>();
        }

        mainMantraButton ??= FindChildComponent<Button>(transform, "MainMantraButton");
        subMantraButton ??= FindChildComponent<Button>(transform, "SubMantraButton");
        guideMantraButton ??= FindChildComponent<Button>(transform, "GuideMantraButton");

        ResolvePartSlotRoot();

        Transform previewStack = FindChildByName(transform, "MantraPreviewStack");
        mainPreviewImage ??= FindChildComponent<Image>(previewStack, "MainPreviewImage");
        subPreviewImage ??= FindChildComponent<Image>(previewStack, "SubPreviewImage");
        guidePreviewImage ??= FindChildComponent<Image>(previewStack, "GuidePreviewImage");

        mainMantraText ??= FindChildComponent<TMP_Text>(transform, "MainMatraText");
        mainMantraText ??= FindChildComponent<TMP_Text>(transform, "MainMantraText");
        subMantraText ??= FindChildComponent<TMP_Text>(transform, "SubMantraText");
        guideMantraText ??= FindChildComponent<TMP_Text>(transform, "GuidMantraText");
        guideMantraText ??= FindChildComponent<TMP_Text>(transform, "GuideMantraText");
    }

    private void ResolvePartSlotRoot()
    {
        Transform nestedSlotRoot = FindChildByName(partSlotRoot, "MantraParts");
        if (nestedSlotRoot != null)
        {
            partSlotRoot = nestedSlotRoot;
            return;
        }

        if (HasDirectPartSlots(partSlotRoot))
        {
            return;
        }

        partSlotRoot = FindChildByName(transform, "MantraParts");
        if (partSlotRoot != null)
        {
            return;
        }

        partSlotRoot = FindChildByName(transform, "MantraPart");
        if (partSlotRoot != null)
        {
            nestedSlotRoot = FindChildByName(partSlotRoot, "MantraParts");
            if (nestedSlotRoot != null)
            {
                partSlotRoot = nestedSlotRoot;
            }

            return;
        }

        partSlotRoot = FindChildByName(transform, "MantraPartSelectionRoot");
    }

    private void SetButtonColor(Button button, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        Graphic graphic = button.targetGraphic;
        if (graphic != null)
        {
            graphic.color = isActive ? activeCategoryColor : normalCategoryColor;
        }
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

    private static bool HasDirectPartSlots(Transform root)
    {
        if (root == null)
        {
            return false;
        }

        foreach (Transform child in root)
        {
            if (child.name.StartsWith("MantraPart_"))
            {
                return true;
            }
        }

        return false;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChildByName(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private sealed class PartSlot
    {
        private readonly GameObject _root;
        private readonly Image _iconImage;
        private readonly Image _backgroundImage;
        private readonly TMP_Text _titleText;
        private readonly TMP_Text _descriptionText;
        private readonly bool _tintBackground;
        private readonly Color _normalColor;
        private readonly Color _selectedColor;
        private readonly Color _emptyColor;

        public PartSlot(GameObject root, bool addMissingButton, bool tintBackground, Color normalColor, Color selectedColor, Color emptyColor)
        {
            _root = root;
            Button = root.GetComponent<Button>();
            if (Button == null && addMissingButton)
            {
                Button = root.AddComponent<Button>();
            }

            _backgroundImage = root.GetComponent<Image>();
            _iconImage = FindChildImage(root.transform) ?? _backgroundImage;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            _titleText = texts.Length > 0 ? texts[0] : null;
            _descriptionText = texts.Length > 1 ? texts[1] : null;
            _tintBackground = tintBackground;
            _normalColor = normalColor;
            _selectedColor = selectedColor;
            _emptyColor = emptyColor;
        }

        public Button Button { get; }

        public void SetPart(MantraPart part, bool selected, bool hideUnused)
        {
            if (_root == null)
            {
                return;
            }

            if (part == null)
            {
                if (hideUnused)
                {
                    _root.SetActive(false);
                    return;
                }

                _root.SetActive(true);
                SetVisual(null, string.Empty, string.Empty, _emptyColor);
                return;
            }

            _root.SetActive(true);
            SetVisual(part.Icon, part.DisplayName, part.Description, selected ? _selectedColor : _normalColor);
        }

        private void SetVisual(Sprite icon, string title, string description, Color color)
        {
            if (_backgroundImage != null && _tintBackground)
            {
                _backgroundImage.color = color;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.color = icon != null ? Color.white : _emptyColor;
                _iconImage.preserveAspect = true;
                _iconImage.enabled = true;
            }

            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }
        }

        private static Image FindChildImage(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image != null && image.transform != root)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
