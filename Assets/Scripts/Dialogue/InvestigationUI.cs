using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component that displays investigation results.
/// The panel can start inactive in the scene; Show activates it and Hide clears then deactivates it.
/// </summary>
public sealed class InvestigationUI : BasePanelUI
{
    private const string TmpPrewarmText = "\uAC00\uB098\uB2E4\uB77C\uB9C8\uBC14\uC0AC\uC544\uC790\uCC28\uCE74\uD0C0\uD30C\uD558 \uC54C\uB9AC\uBC14\uC774 \uC870\uC0AC \uB300\uD654 \uB3CC\uC544\uAC00\uAE30";
    private const string PortraitPrewarmText = "AB \uC62C\uBE7C\uBBF8 \uD0D0\uC815 \uC950 \uC870\uC218 \uB300\uD654\uC0C1\uB300";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private DialogueLog dialogueLog;

    [Header("Dialogue Portraits")]
    [SerializeField] private Sprite playerPortraitSprite;
    [SerializeField] private Sprite assistantPortraitSprite;
    [SerializeField] private Sprite defaultNpcPortraitSprite;
    [SerializeField] private PortraitSpriteBinding[] customPortraitSprites = Array.Empty<PortraitSpriteBinding>();

    [Header("Temporary Portrait Slots")]
    [SerializeField] private bool showTemporaryPortraitSlots = true;
    [SerializeField] private Vector2 portraitSlotSize = new Vector2(260f, 320f);
    [SerializeField] private float portraitBottomOffset = 250f;
    [SerializeField] private float portraitSidePadding = 80f;
    [SerializeField, Range(0.1f, 1f)] private float inactivePortraitAlpha = 0.45f;

    private readonly List<DialogueLine> _lines = new();
    private int _currentLineIndex;
    private RectTransform _leftPortraitRoot;
    private RectTransform _rightPortraitRoot;
    private CanvasGroup _leftPortraitGroup;
    private CanvasGroup _rightPortraitGroup;
    private Image _leftPortraitImage;
    private Image _rightPortraitImage;
    private TMP_Text _leftPortraitLabel;
    private TMP_Text _rightPortraitLabel;
    private TMP_Text _leftPortraitName;
    private TMP_Text _rightPortraitName;
    private string _leftSpeakerName = "\uC62C\uBE7C\uBBF8 \uD0D0\uC815";
    private string _rightSpeakerName = "\uB300\uD654\uC0C1\uB300";
    private string _leftPortraitKey = "player";
    private string _rightPortraitKey = "";

    public bool HasNextLine => IsVisible && _currentLineIndex + 1 < _lines.Count;
    public int LastShownFrame { get; private set; } = -1;

    protected override void Awake()
    {
        base.Awake();

        TmpTextPrewarmUtility.Prewarm(titleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(bodyText, TmpPrewarmText);
        BuildPortraitSlots();
        if (dialogueLog == null)
        {
            dialogueLog = FindFirstObjectByType<DialogueLog>();
        }

        ClearTexts();
    }

    public void Show(string title, string body)
    {
        ShowSequence(new[] { new DialogueLine(title, body) });
    }

    public void ShowSequence(IEnumerable<DialogueLine> lines)
    {
        _lines.Clear();
        if (lines != null)
        {
            foreach (DialogueLine line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line.Text))
                {
                    _lines.Add(line);
                }
            }
        }

        if (_lines.Count == 0)
        {
            Hide();
            return;
        }

        _currentLineIndex = 0;
        ResolvePortraitSpeakerNames();
        LastShownFrame = Time.frameCount;
        base.Show();
        SetPortraitSlotsVisible(showTemporaryPortraitSlots);
        RenderCurrentLine();
    }

    public void ShowNextLine()
    {
        if (!HasNextLine)
        {
            return;
        }

        _currentLineIndex++;
        RenderCurrentLine();
    }

    public override void Hide()
    {
        _lines.Clear();
        _currentLineIndex = 0;
        ClearTexts();
        SetPortraitSlotsVisible(false);
        base.Hide();
    }

    private void RenderCurrentLine()
    {
        DialogueLine line = _lines[_currentLineIndex];

        if (titleText != null)
        {
            titleText.text = line.Speaker;
        }

        if (bodyText != null)
        {
            bodyText.text = line.Text;
        }

        RefreshPortraitSlots(line);

        if (dialogueLog == null)
        {
            dialogueLog = FindFirstObjectByType<DialogueLog>();
        }

        dialogueLog?.Add(line);
    }

    private void ClearTexts()
    {
        if (titleText != null)
        {
            titleText.text = string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }
    }

    private void BuildPortraitSlots()
    {
        if (!showTemporaryPortraitSlots || panelRoot == null || _leftPortraitRoot != null)
        {
            return;
        }

        Transform parent = panelRoot.transform.parent != null ? panelRoot.transform.parent : panelRoot.transform;
        _leftPortraitRoot = CreatePortraitSlot(parent, "DialoguePortrait_A", false, out _leftPortraitGroup, out _leftPortraitImage, out _leftPortraitLabel, out _leftPortraitName);
        _rightPortraitRoot = CreatePortraitSlot(parent, "DialoguePortrait_B", true, out _rightPortraitGroup, out _rightPortraitImage, out _rightPortraitLabel, out _rightPortraitName);
        SetPortraitSlotsVisible(false);
    }

    private RectTransform CreatePortraitSlot(
        Transform parent,
        string objectName,
        bool rightSide,
        out CanvasGroup canvasGroup,
        out Image portraitImage,
        out TMP_Text labelText,
        out TMP_Text nameText)
    {
        GameObject rootObject = new GameObject(objectName);
        rootObject.transform.SetParent(parent, false);

        RectTransform root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = rightSide ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        root.anchorMax = rightSide ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        root.pivot = rightSide ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        root.sizeDelta = portraitSlotSize;
        root.anchoredPosition = new Vector2(rightSide ? -portraitSidePadding : portraitSidePadding, portraitBottomOffset);

        Image background = rootObject.AddComponent<Image>();
        background.color = new Color(0.86f, 0.86f, 0.82f, 0.58f);
        background.raycastTarget = false;

        canvasGroup = rootObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = inactivePortraitAlpha;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject portraitObject = new GameObject("PortraitImage");
        portraitObject.transform.SetParent(rootObject.transform, false);
        RectTransform portraitRect = portraitObject.AddComponent<RectTransform>();
        portraitRect.anchorMin = Vector2.zero;
        portraitRect.anchorMax = Vector2.one;
        portraitRect.offsetMin = new Vector2(10f, 52f);
        portraitRect.offsetMax = new Vector2(-10f, -10f);

        portraitImage = portraitObject.AddComponent<Image>();
        portraitImage.color = Color.white;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        portraitImage.enabled = false;

        GameObject labelObject = new GameObject("SlotLabel");
        labelObject.transform.SetParent(rootObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 120f;
        labelText.color = new Color(0.16f, 0.14f, 0.12f, 0.82f);
        labelText.raycastTarget = false;
        labelText.text = rightSide ? "B" : "A";

        GameObject nameObject = new GameObject("SpeakerName");
        nameObject.transform.SetParent(rootObject.transform, false);
        RectTransform nameRect = nameObject.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 12f);
        nameRect.sizeDelta = new Vector2(0f, 40f);

        nameText = nameObject.AddComponent<TextMeshProUGUI>();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 24f;
        nameText.color = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        nameText.raycastTarget = false;
        nameText.text = string.Empty;

        TMP_FontAsset sceneFont = titleText != null ? titleText.font : bodyText != null ? bodyText.font : null;
        if (sceneFont != null)
        {
            labelText.font = sceneFont;
            nameText.font = sceneFont;
        }

        TmpTextPrewarmUtility.Prewarm(labelText, PortraitPrewarmText);
        TmpTextPrewarmUtility.Prewarm(nameText, PortraitPrewarmText);
        return root;
    }

    private void RefreshPortraitSlots(DialogueLine currentLine)
    {
        if (!showTemporaryPortraitSlots || _leftPortraitRoot == null || _rightPortraitRoot == null)
        {
            return;
        }

        string subject = string.IsNullOrWhiteSpace(currentLine.Speaker) ? string.Empty : currentLine.Speaker;
        bool leftIsActive = IsPlayerSpeaker(subject);

        string currentPortraitKey = string.IsNullOrWhiteSpace(currentLine.PortraitKey) ? string.Empty : currentLine.PortraitKey;
        Sprite leftSprite = leftIsActive && !string.IsNullOrWhiteSpace(currentPortraitKey)
            ? ResolvePortraitSprite(currentPortraitKey, subject)
            : ResolvePortraitSprite(_leftPortraitKey, _leftSpeakerName);
        Sprite rightSprite = !leftIsActive && !string.IsNullOrWhiteSpace(currentPortraitKey)
            ? ResolvePortraitSprite(currentPortraitKey, subject)
            : ResolvePortraitSprite(_rightPortraitKey, _rightSpeakerName);

        ApplyPortrait(_leftPortraitImage, _leftPortraitLabel, leftSprite, "A");
        ApplyPortrait(_rightPortraitImage, _rightPortraitLabel, rightSprite, "B");

        if (_leftPortraitName != null)
        {
            _leftPortraitName.text = _leftSpeakerName;
        }

        if (_rightPortraitName != null)
        {
            _rightPortraitName.text = _rightSpeakerName;
        }

        SetPortraitFocus(leftIsActive);
    }

    private static void ApplyPortrait(Image portraitImage, TMP_Text fallbackLabel, Sprite sprite, string fallbackText)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        if (fallbackLabel != null)
        {
            fallbackLabel.gameObject.SetActive(sprite == null);
            fallbackLabel.text = fallbackText;
        }
    }

    private Sprite ResolvePortraitSprite(string portraitKey, string speaker)
    {
        if (!string.IsNullOrWhiteSpace(portraitKey))
        {
            foreach (PortraitSpriteBinding binding in customPortraitSprites ?? Array.Empty<PortraitSpriteBinding>())
            {
                if (binding != null && binding.Matches(portraitKey) && binding.Sprite != null)
                {
                    return binding.Sprite;
                }
            }

            if (IsPlayerPortraitKey(portraitKey))
            {
                return playerPortraitSprite;
            }

            if (IsAssistantPortraitKey(portraitKey))
            {
                return assistantPortraitSprite;
            }
        }

        if (IsPlayerSpeaker(speaker))
        {
            return playerPortraitSprite;
        }

        if (IsAssistantSpeaker(speaker))
        {
            return assistantPortraitSprite;
        }

        return defaultNpcPortraitSprite;
    }

    private void ResolvePortraitSpeakerNames()
    {
        _leftSpeakerName = "\uC62C\uBE7C\uBBF8 \uD0D0\uC815";
        _rightSpeakerName = "\uB300\uD654\uC0C1\uB300";
        _leftPortraitKey = "player";
        _rightPortraitKey = "";

        foreach (DialogueLine line in _lines)
        {
            string speaker = line.Speaker;
            if (string.IsNullOrWhiteSpace(speaker))
            {
                continue;
            }

            if (IsPlayerSpeaker(speaker))
            {
                _leftSpeakerName = NormalizePlayerSpeakerName(speaker);
                _leftPortraitKey = string.IsNullOrWhiteSpace(line.PortraitKey) ? "player" : line.PortraitKey;
                break;
            }
        }

        foreach (DialogueLine line in _lines)
        {
            string speaker = line.Speaker;
            if (!string.IsNullOrWhiteSpace(speaker) && !IsPlayerSpeaker(speaker))
            {
                _rightSpeakerName = speaker;
                _rightPortraitKey = line.PortraitKey;
                break;
            }
        }
    }

    private static bool IsPlayerPortraitKey(string portraitKey)
    {
        return string.Equals(portraitKey, "player", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "owl", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "owl_detective", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "private_investigator", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAssistantPortraitKey(string portraitKey)
    {
        return string.Equals(portraitKey, "assistant", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "rat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "rat_assistant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlayerSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return false;
        }

        return speaker.Contains("\uB098") ||
               speaker.Contains("\uD50C\uB808\uC774\uC5B4") ||
               speaker.Contains("\uD0D0\uC815") ||
               speaker.Contains("\uC62C\uBE7C\uBBF8");
    }

    private static bool IsAssistantSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return false;
        }

        return speaker.Contains("\uC870\uC218") ||
               speaker.Contains("\uC950");
    }

    private static string NormalizePlayerSpeakerName(string speaker)
    {
        return string.IsNullOrWhiteSpace(speaker) || speaker == "\uB098" ? "\uC62C\uBE7C\uBBF8 \uD0D0\uC815" : speaker;
    }

    private void SetPortraitFocus(bool leftIsActive)
    {
        if (_leftPortraitGroup != null)
        {
            _leftPortraitGroup.alpha = leftIsActive ? 1f : inactivePortraitAlpha;
        }

        if (_rightPortraitGroup != null)
        {
            _rightPortraitGroup.alpha = leftIsActive ? inactivePortraitAlpha : 1f;
        }
    }

    private void SetPortraitSlotsVisible(bool visible)
    {
        if (_leftPortraitRoot != null)
        {
            _leftPortraitRoot.gameObject.SetActive(visible);
        }

        if (_rightPortraitRoot != null)
        {
            _rightPortraitRoot.gameObject.SetActive(visible);
        }
    }

    [Serializable]
    private sealed class PortraitSpriteBinding
    {
        [SerializeField] private string portraitKey;
        [SerializeField] private Sprite sprite;

        public Sprite Sprite => sprite;

        public bool Matches(string key)
        {
            return !string.IsNullOrWhiteSpace(portraitKey) &&
                   string.Equals(portraitKey.Trim(), key?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
