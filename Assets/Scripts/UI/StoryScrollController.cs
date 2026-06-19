using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class StoryScrollController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_FontAsset defaultFont;

    [Header("Prefabs")]
    [SerializeField] private GameObject textBlockPrefab;
    [SerializeField] private GameObject imageBlockPrefab;
    [SerializeField] private GameObject choiceBlockPrefab;

    [Header("Scrolling")]
    [SerializeField] private bool autoScrollToBottom = true;

    [Header("Text Blocks")]
    [SerializeField] private float storyFontSize = 36f;
    [SerializeField] private Color storyTextColor = new Color(0.92f, 0.93f, 0.9f, 1f);

    [Header("Illustration Blocks")]
    [SerializeField] private float defaultIllustrationHeight = 420f;
    [SerializeField] private Color illustrationFallbackColor = new Color(1f, 1f, 1f, 0.08f);

    [Header("Choice Blocks")]
    [SerializeField] private float choiceButtonHeight = 88f;
    [SerializeField] private float choiceSpacing = 16f;
    [SerializeField] private Color choiceNormalColor = new Color(0.08f, 0.2f, 0.3f, 1f);
    [SerializeField] private Color choiceTextColor = Color.white;

    private Coroutine _scrollRoutine;
    private bool _scrollToBottomDirty;

    public RectTransform ContentRoot => contentRoot;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public TMP_Text AddStoryText(string text)
    {
        ResolveReferences();

        if (textBlockPrefab != null)
        {
            GameObject prefabBlock = InstantiateBlock(textBlockPrefab, "TextBlock");
            TMP_Text prefabLabel = prefabBlock.GetComponentInChildren<TMP_Text>(true);
            if (prefabLabel == null)
            {
                Debug.LogWarning($"{nameof(StoryScrollController)}: TextBlock prefab needs a TMP_Text component.", prefabBlock);
                RequestScrollToBottom();
                return null;
            }

            prefabLabel.text = text;
            RequestScrollToBottom();
            return prefabLabel;
        }

        GameObject block = new GameObject("StoryTextBlock", typeof(RectTransform));
        block.transform.SetParent(contentRoot, false);

        TMP_Text label = block.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.text = text;
        label.fontSize = storyFontSize;
        label.color = storyTextColor;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        ContentSizeFitter fitter = block.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement layout = block.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;

        RequestScrollToBottom();
        return label;
    }

    public Image AddIllustration(Sprite sprite, float height = -1f)
    {
        ResolveReferences();

        if (imageBlockPrefab != null)
        {
            GameObject prefabBlock = InstantiateBlock(imageBlockPrefab, "ImageBlock");
            Image prefabImage = prefabBlock.GetComponentInChildren<Image>(true);
            if (prefabImage == null)
            {
                Debug.LogWarning($"{nameof(StoryScrollController)}: ImageBlock prefab needs an Image component.", prefabBlock);
                RequestScrollToBottom();
                return null;
            }

            prefabImage.sprite = sprite;
            if (sprite == null)
            {
                prefabImage.color = illustrationFallbackColor;
            }

            if (height > 0f && prefabBlock.TryGetComponent(out LayoutElement prefabLayout))
            {
                prefabLayout.preferredHeight = height;
            }

            RequestScrollToBottom();
            return prefabImage;
        }

        GameObject block = new GameObject("IllustrationBlock", typeof(RectTransform));
        block.transform.SetParent(contentRoot, false);

        Image image = block.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : illustrationFallbackColor;
        image.preserveAspect = true;
        image.raycastTarget = false;

        LayoutElement layout = block.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.preferredHeight = height > 0f ? height : defaultIllustrationHeight;

        RequestScrollToBottom();
        return image;
    }

    public IReadOnlyList<Button> AddChoices(IReadOnlyList<string> choices, UnityAction<int, string> onChoiceSelected = null)
    {
        ResolveReferences();

        if (choiceBlockPrefab != null)
        {
            IReadOnlyList<Button> prefabButtons = AddChoicesFromPrefab(choices, onChoiceSelected);
            RequestScrollToBottom();
            return prefabButtons;
        }

        GameObject block = new GameObject("ChoiceBlock", typeof(RectTransform));
        block.transform.SetParent(contentRoot, false);

        VerticalLayoutGroup layoutGroup = block.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = choiceSpacing;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = block.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        List<Button> buttons = new List<Button>();
        if (choices != null)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                buttons.Add(CreateChoiceButton(block.transform, i, choices[i], onChoiceSelected));
            }
        }

        RequestScrollToBottom();
        return buttons;
    }

    public void Clear()
    {
        ResolveReferences();

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        ScrollToTop();
    }

    public void ScrollToTop()
    {
        ResolveReferences();
        StopScrollRoutine();
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    public void ScrollToBottom()
    {
        ResolveReferences();
        QueueScrollToBottom();
    }

    [ContextMenu("Add Demo Content")]
    private void AddDemoContent()
    {
        AddStoryText("You stand before an old gate.\nCold wind slips through your armor.");
        AddIllustration(null);
        AddChoices(new[] { "Knock on the gate", "Look around", "Wait for a moment" });
    }

    private Button CreateChoiceButton(Transform parent, int index, string choiceText, UnityAction<int, string> onChoiceSelected)
    {
        GameObject buttonObject = new GameObject($"ChoiceButton_{index + 1}", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.AddComponent<Image>();
        background.color = choiceNormalColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        if (onChoiceSelected != null)
        {
            string capturedText = choiceText;
            int capturedIndex = index;
            button.onClick.AddListener(() => onChoiceSelected.Invoke(capturedIndex, capturedText));
        }

        LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = choiceButtonHeight;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);

        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.text = choiceText;
        label.fontSize = storyFontSize * 0.86f;
        label.color = choiceTextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;

        return button;
    }

    private IReadOnlyList<Button> AddChoicesFromPrefab(IReadOnlyList<string> choices, UnityAction<int, string> onChoiceSelected)
    {
        GameObject block = InstantiateBlock(choiceBlockPrefab, "ChoiceBlock");
        Button[] buttons = block.GetComponentsInChildren<Button>(true);
        List<Button> activeButtons = new List<Button>();
        int choiceCount = choices?.Count ?? 0;

        if (buttons.Length == 0)
        {
            Debug.LogWarning($"{nameof(StoryScrollController)}: ChoiceBlock prefab needs at least one Button component.", block);
            return activeButtons;
        }

        EnsureChoiceButtonCount(block.transform, buttons, choiceCount);
        buttons = block.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            bool shouldShow = i < choiceCount;
            button.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                continue;
            }

            string choiceText = choices[i];
            button.name = $"ChoiceButton_{i + 1}";
            SetButtonText(button, choiceText);
            button.onClick.RemoveAllListeners();

            if (onChoiceSelected != null)
            {
                int capturedIndex = i;
                string capturedText = choiceText;
                button.onClick.AddListener(() => onChoiceSelected.Invoke(capturedIndex, capturedText));
            }

            activeButtons.Add(button);
        }

        return activeButtons;
    }

    private static void EnsureChoiceButtonCount(Transform blockRoot, Button[] buttons, int choiceCount)
    {
        if (choiceCount <= buttons.Length || buttons.Length == 0)
        {
            return;
        }

        Button template = buttons[buttons.Length - 1];
        Transform parent = template.transform.parent != null ? template.transform.parent : blockRoot;

        for (int i = buttons.Length; i < choiceCount; i++)
        {
            Button copy = Instantiate(template, parent);
            copy.name = $"ChoiceButton_{i + 1}";
        }
    }

    private static void SetButtonText(Button button, string text)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

    private GameObject InstantiateBlock(GameObject prefab, string fallbackName)
    {
        GameObject block = Instantiate(prefab, contentRoot, false);
        block.name = string.IsNullOrWhiteSpace(prefab.name) ? fallbackName : prefab.name;
        return block;
    }

    private void RequestScrollToBottom()
    {
        if (!autoScrollToBottom)
        {
            return;
        }

        ResolveReferences();
        QueueScrollToBottom();
    }

    private void QueueScrollToBottom()
    {
        _scrollToBottomDirty = true;

        if (_scrollRoutine == null)
        {
            _scrollRoutine = StartCoroutine(ScrollToBottomRoutine());
        }
    }

    private IEnumerator ScrollToBottomRoutine()
    {
        while (_scrollToBottomDirty)
        {
            _scrollToBottomDirty = false;

            yield return null;
            RebuildScrollLayout();

            yield return null;
            RebuildScrollLayout();

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
            scrollRect.velocity = Vector2.zero;
        }

        _scrollRoutine = null;
    }

    private void StopScrollRoutine()
    {
        if (_scrollRoutine == null)
        {
            return;
        }

        _scrollToBottomDirty = false;
        StopCoroutine(_scrollRoutine);
        _scrollRoutine = null;
    }

    private void RebuildScrollLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }
    }

    private void ResolveReferences()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (contentRoot == null && scrollRect != null)
        {
            contentRoot = scrollRect.content;
        }
    }

    private void ApplyFont(TMP_Text label)
    {
        if (defaultFont != null)
        {
            label.font = defaultFont;
        }
    }
}
