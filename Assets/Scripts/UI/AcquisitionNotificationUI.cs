using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AcquisitionNotificationUI : MonoBehaviour
{
    private const string TmpPrewarmText = "키워드 획득 단서 획득 알리바이 바뀐 약 단서를 획득했습니다";

    [SerializeField] private EvidenceInventory evidenceInventory;
    [SerializeField] private float visibleSeconds = 1.45f;
    [SerializeField] private float fadeSeconds = 0.18f;

    private readonly List<NotificationMessage> _messages = new();
    private CanvasGroup _canvasGroup;
    private TMP_Text _messageText;
    private Coroutine _displayRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (FindFirstObjectByType<AcquisitionNotificationUI>() != null)
        {
            return;
        }

        GameObject notificationObject = new("AcquisitionNotificationUI");
        notificationObject.AddComponent<AcquisitionNotificationUI>();
    }

    private void Awake()
    {
        ResolveReferences();
        BuildRuntimeUI();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindInventory(true);
    }

    private void OnDisable()
    {
        BindInventory(false);
    }

    private void ResolveReferences()
    {
        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }
    }

    private void BindInventory(bool bind)
    {
        if (evidenceInventory == null)
        {
            return;
        }

        if (bind)
        {
            evidenceInventory.EvidenceAdded += HandleEvidenceAdded;
            evidenceInventory.CsvEvidenceAdded += HandleCsvEvidenceAdded;
            evidenceInventory.KeywordAdded += HandleKeywordAdded;
            evidenceInventory.CsvKeywordAdded += HandleCsvKeywordAdded;
        }
        else
        {
            evidenceInventory.EvidenceAdded -= HandleEvidenceAdded;
            evidenceInventory.CsvEvidenceAdded -= HandleCsvEvidenceAdded;
            evidenceInventory.KeywordAdded -= HandleKeywordAdded;
            evidenceInventory.CsvKeywordAdded -= HandleCsvKeywordAdded;
        }
    }

    private void BuildRuntimeUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("NotificationCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            canvasObject.AddComponent<CanvasScaler>();
        }

        Transform existing = canvas.transform.Find("AcquisitionNotificationPanel");
        GameObject panel = existing != null ? existing.gameObject : new GameObject("AcquisitionNotificationPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();

        RectTransform panelRect = GetOrAdd<RectTransform>(panel);
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 210f);

        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = new Color(0.95f, 0.95f, 0.95f, 0.96f);
        panelImage.raycastTarget = false;

        _canvasGroup = GetOrAdd<CanvasGroup>(panel);
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        Transform textTransform = panel.transform.Find("Message");
        GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Message");
        textObject.transform.SetParent(panel.transform, false);

        RectTransform textRect = GetOrAdd<RectTransform>(textObject);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(64f, 18f);
        textRect.offsetMax = new Vector2(-64f, -18f);

        _messageText = GetOrAdd<TextMeshProUGUI>(textObject);
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.color = Color.black;
        _messageText.fontSize = 56f;
        _messageText.enableAutoSizing = true;
        _messageText.fontSizeMin = 30f;
        _messageText.fontSizeMax = 64f;
        _messageText.raycastTarget = false;

        TMP_FontAsset sceneFont = FindSceneFont();
        if (sceneFont != null)
        {
            _messageText.font = sceneFont;
        }

        TmpTextPrewarmUtility.Prewarm(_messageText, TmpPrewarmText);
        panel.SetActive(true);
    }

    private void HandleEvidenceAdded(EvidenceData evidence)
    {
        if (evidence != null)
        {
            Enqueue("단서 획득", $"{evidence.DisplayName} 단서를 획득했습니다.");
        }
    }

    private void HandleCsvEvidenceAdded(CsvEvidenceRecord evidence)
    {
        if (evidence != null)
        {
            Enqueue("단서 획득", $"{evidence.DisplayName} 단서를 획득했습니다.");
        }
    }

    private void HandleKeywordAdded(KeywordData keyword)
    {
        if (keyword != null)
        {
            Enqueue("키워드 획득", $"{keyword.DisplayName} 키워드를 획득했습니다.");
        }
    }

    private void HandleCsvKeywordAdded(CsvKeywordRecord keyword)
    {
        if (keyword != null)
        {
            Enqueue("키워드 획득", $"{keyword.DisplayName} 키워드를 획득했습니다.");
        }
    }

    private void Enqueue(string title, string body)
    {
        int priority = title == "단서 획득" ? 0 : 1;
        _messages.Add(new NotificationMessage(title, body, priority));
        if (_displayRoutine == null)
        {
            _displayRoutine = StartCoroutine(DisplayQueuedMessages());
        }
    }

    private IEnumerator DisplayQueuedMessages()
    {
        yield return null;

        while (_messages.Count > 0)
        {
            NotificationMessage message = TakeNextMessage();
            if (_messageText != null)
            {
                _messageText.text = $"{message.Title}\n{message.Body}";
            }

            yield return FadeTo(1f);
            yield return new WaitForSecondsRealtime(visibleSeconds);
            yield return FadeTo(0f);
        }

        _displayRoutine = null;
    }

    private NotificationMessage TakeNextMessage()
    {
        int selectedIndex = 0;
        for (int i = 1; i < _messages.Count; i++)
        {
            if (_messages[i].Priority < _messages[selectedIndex].Priority)
            {
                selectedIndex = i;
            }
        }

        NotificationMessage message = _messages[selectedIndex];
        _messages.RemoveAt(selectedIndex);
        return message;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (_canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeSeconds);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    private static TMP_FontAsset FindSceneFont()
    {
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text != null && text.font != null)
            {
                return text.font;
            }
        }

        return null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        if (!target.TryGetComponent(out T component))
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private readonly struct NotificationMessage
    {
        public NotificationMessage(string title, string body, int priority)
        {
            Title = title;
            Body = body;
            Priority = priority;
        }

        public string Title { get; }
        public string Body { get; }
        public int Priority { get; }
    }
}
