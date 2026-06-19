using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// UI component that displays investigation results.
/// The panel can start inactive in the scene; Show activates it and Hide clears then deactivates it.
/// </summary>
public sealed class InvestigationUI : BasePanelUI
{
    private const string TmpPrewarmText = "\uAC00\uB098\uB2E4\uB77C\uB9C8\uBC14\uC0AC\uC544\uC790\uCC28\uCE74\uD0C0\uD30C\uD558 \uC54C\uB9AC\uBC14\uC774 \uC870\uC0AC \uB300\uD654 \uB3CC\uC544\uAC00\uAE30";
    private const string PortraitPrewarmText = "AB \uC190\uC8FC \uC62C\uBE7C\uBBF8 \uD0D0\uC815 \uD560\uC544\uBC84\uC9C0 \uC62C\uBE7C\uBBF8 \uD0D0\uC815 \uB300\uD654\uC0C1\uB300";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private DialogueLog dialogueLog;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;

    [Header("Dialogue Portraits")]
    [SerializeField] private Sprite playerPortraitSprite;
    [SerializeField] private Sprite assistantPortraitSprite;
    [SerializeField] private Sprite defaultNpcPortraitSprite;
    [SerializeField] private PortraitSpriteRegistry portraitSpriteRegistry;
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
    private string _leftSpeakerName = "\uC190\uC8FC \uC62C\uBE7C\uBBF8";
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

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        if (portraitSpriteRegistry == null)
        {
            portraitSpriteRegistry = FindFirstObjectByType<PortraitSpriteRegistry>();
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
        SetPortraitSlotsVisible(ShouldShowPortraitSlots(_lines[0]));
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
            titleText.text = ResolveSpeakerDisplayName(line.Speaker);
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

        if (!ShouldShowPortraitSlots(currentLine))
        {
            SetPortraitSlotsVisible(false);
            return;
        }

        SetPortraitSlotsVisible(true);

        string subject = string.IsNullOrWhiteSpace(currentLine.Speaker) ? string.Empty : currentLine.Speaker;
        bool leftIsActive = IsPlayerSpeaker(subject);

        string currentPortraitKey = ResolvePortraitKey(currentLine);
        Sprite leftSprite = leftIsActive && !string.IsNullOrWhiteSpace(currentPortraitKey)
            ? ResolvePortraitSprite(currentPortraitKey, subject)
            : ResolvePortraitSprite(_leftPortraitKey, _leftSpeakerName);
        Sprite rightSprite = !leftIsActive && !string.IsNullOrWhiteSpace(currentPortraitKey)
            ? ResolvePortraitSprite(currentPortraitKey, subject)
            : ResolvePortraitSprite(_rightPortraitKey, _rightSpeakerName);

        ApplyPortrait(_leftPortraitImage, _leftPortraitLabel, leftSprite, "A");
        ApplyPortrait(_rightPortraitImage, _rightPortraitLabel, rightSprite, "B");
        ApplyPortraitLayout(currentLine);

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

    private void ApplyPortraitLayout(DialogueLine currentLine)
    {
        bool singleLeft = IsSinglePortraitLayout(currentLine.PortraitLayout);

        if (_leftPortraitRoot != null)
        {
            _leftPortraitRoot.gameObject.SetActive(true);
        }

        if (_rightPortraitRoot != null)
        {
            _rightPortraitRoot.gameObject.SetActive(!singleLeft);
        }
    }

    private Sprite ResolvePortraitSprite(string portraitKey, string speaker)
    {
        if (!string.IsNullOrWhiteSpace(portraitKey))
        {
            if (portraitSpriteRegistry == null)
            {
                portraitSpriteRegistry = FindFirstObjectByType<PortraitSpriteRegistry>();
            }

            if (portraitSpriteRegistry != null && portraitSpriteRegistry.TryGetSprite(portraitKey, out Sprite registrySprite))
            {
                return registrySprite;
            }

            foreach (PortraitSpriteBinding binding in customPortraitSprites ?? Array.Empty<PortraitSpriteBinding>())
            {
                if (binding != null && binding.Matches(portraitKey) && binding.Sprite != null)
                {
                    return binding.Sprite;
                }
            }

            if (IsPlayerPortraitKey(portraitKey))
            {
                return playerPortraitSprite != null ? playerPortraitSprite : ResolveEditorPortraitFallback(portraitKey);
            }

            if (IsAssistantPortraitKey(portraitKey))
            {
                return assistantPortraitSprite != null ? assistantPortraitSprite : ResolveEditorPortraitFallback(portraitKey);
            }
        }

        if (IsPlayerSpeaker(speaker))
        {
            return playerPortraitSprite != null ? playerPortraitSprite : ResolveEditorPortraitFallback("child_owl");
        }

        if (IsAssistantSpeaker(speaker))
        {
            return assistantPortraitSprite != null ? assistantPortraitSprite : ResolveEditorPortraitFallback("old_owl");
        }

        return defaultNpcPortraitSprite;
    }

    private string ResolveSpeakerDisplayName(string speakerKey)
    {
        if (string.IsNullOrWhiteSpace(speakerKey))
        {
            return string.Empty;
        }

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        return dialogueDatabase != null ? dialogueDatabase.ResolveSpeakerDisplayName(speakerKey) : speakerKey;
    }

    private string ResolvePortraitKey(DialogueLine line, string fallbackPortraitKey = "")
    {
        if (!string.IsNullOrWhiteSpace(line.PortraitKey))
        {
            return line.PortraitKey;
        }

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }

        string defaultPortraitKey = dialogueDatabase != null
            ? dialogueDatabase.ResolveDefaultPortraitKey(line.Speaker)
            : string.Empty;

        return string.IsNullOrWhiteSpace(defaultPortraitKey) ? fallbackPortraitKey : defaultPortraitKey;
    }

    private static Sprite ResolveEditorPortraitFallback(string portraitKey)
    {
#if UNITY_EDITOR
        string path = string.Empty;
        if (IsPlayerPortraitKey(portraitKey))
        {
            path = "Assets/Sprites/Child_Owl.png";
        }
        else if (IsAssistantPortraitKey(portraitKey))
        {
            path = "Assets/Sprites/Old_Owl.png";
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif

        return null;
    }

    private void ResolvePortraitSpeakerNames()
    {
        _leftSpeakerName = "\uC190\uC8FC \uC62C\uBE7C\uBBF8";
        _rightSpeakerName = "\uB300\uD654\uC0C1\uB300";
        _leftPortraitKey = "player";
        _rightPortraitKey = "";

        foreach (DialogueLine line in _lines)
        {
            if (!ShouldShowPortraitSlots(line))
            {
                continue;
            }

            string speaker = line.Speaker;
            if (string.IsNullOrWhiteSpace(speaker))
            {
                continue;
            }

            if (IsPlayerSpeaker(speaker))
            {
                _leftSpeakerName = NormalizePlayerSpeakerName(speaker);
                _leftPortraitKey = ResolvePortraitKey(line, "player");
                break;
            }
        }

        foreach (DialogueLine line in _lines)
        {
            if (!ShouldShowPortraitSlots(line))
            {
                continue;
            }

            string speaker = line.Speaker;
            if (!string.IsNullOrWhiteSpace(speaker) && !IsPlayerSpeaker(speaker))
            {
                _rightSpeakerName = ResolveSpeakerDisplayName(speaker);
                _rightPortraitKey = ResolvePortraitKey(line);
                break;
            }
        }
    }

    private static bool IsPlayerPortraitKey(string portraitKey)
    {
        return string.Equals(portraitKey, "player", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "child", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "child_owl", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "rookie_owl", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "owl", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "owl_detective", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "private_investigator", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAssistantPortraitKey(string portraitKey)
    {
        return string.Equals(portraitKey, "assistant", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "old_owl", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "grandfather", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "mentor", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "rat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "rat_assistant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlayerSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return false;
        }

        if (speaker.Equals("child_owl", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("player", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("player.child_owl", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("rookie_owl", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (speaker.Equals("old_owl", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("grandfather", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("npc.old_owl", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("system", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("system_narration", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("system.narration", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("narration", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (speaker.Contains("\uD560\uC544\uBC84\uC9C0") ||
            speaker.Contains("\uC6D0\uB85C") ||
            speaker.Contains("\uC2A4\uC2B9"))
        {
            return false;
        }

        return speaker.Contains("\uB098") ||
               speaker.Contains("\uD50C\uB808\uC774\uC5B4") ||
               speaker.Contains("\uC62C\uBE7C\uBBF8 \uD0D0\uC815") ||
               speaker.Contains("\uC190\uC8FC") ||
               speaker.Contains("\uCD08\uC9DC");
    }

    private static bool IsAssistantSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return false;
        }

        if (speaker.Equals("old_owl", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("grandfather", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("npc.old_owl", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return speaker.Contains("\uC870\uC218") ||
               speaker.Contains("\uC950") ||
               speaker.Contains("\uD560\uC544\uBC84\uC9C0") ||
               speaker.Contains("\uC6D0\uB85C") ||
               speaker.Contains("\uC2A4\uC2B9");
    }

    private static string NormalizePlayerSpeakerName(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker) || speaker == "\uB098")
        {
            return "\uC190\uC8FC \uC62C\uBE7C\uBBF8";
        }

        if (speaker.Equals("child_owl", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("player", StringComparison.OrdinalIgnoreCase) ||
            speaker.Equals("player.child_owl", StringComparison.OrdinalIgnoreCase))
        {
            return "\uC190\uC8FC \uC62C\uBE7C\uBBF8";
        }

        return speaker;
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

    private bool ShouldShowPortraitSlots(DialogueLine line)
    {
        return showTemporaryPortraitSlots && line.ShowPortraits && !IsPortraitSuppressedKey(line.PortraitKey);
    }

    private static bool IsSinglePortraitLayout(string portraitLayout)
    {
        return string.Equals(portraitLayout, "single", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitLayout, "left", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitLayout, "player_only", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPortraitSuppressedKey(string portraitKey)
    {
        return string.Equals(portraitKey, "none", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "off", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "narration", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(portraitKey, "system", StringComparison.OrdinalIgnoreCase);
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
