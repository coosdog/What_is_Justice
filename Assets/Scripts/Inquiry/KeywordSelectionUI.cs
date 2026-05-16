using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class KeywordSelectionUI : BasePanelUI
{
    private const string TmpPrewarmText = "\uD0A4\uC6CC\uB4DC \uBB34\uC5C7\uC744 \uBB3C\uC5B4\uBCFC\uAE4C \uB3CC\uC544\uAC00\uAE30 \uC54C\uB9AC\uBC14\uC774";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button keywordButtonPrefab;
    [SerializeField] private Button cancelButton;

    private readonly List<Button> _spawnedButtons = new();
    private Action<KeywordData> _keywordSelected;
    private Action<CsvKeywordRecord> _csvKeywordSelected;
    private bool _cancelBound;

    protected override void Awake()
    {
        base.Awake();
        BindCancelButton();
        PrewarmTexts();
        Hide();
    }

    public void Show(string title, IEnumerable<KeywordData> keywords, Action<KeywordData> keywordSelected)
    {
        if (!HasRequiredReferences())
        {
            Debug.LogWarning("KeywordSelectionUI is missing scene references.");
            return;
        }

        BindCancelButton();
        ClearButtons();
        _keywordSelected = keywordSelected;
        _csvKeywordSelected = null;
        SetTitle(title);

        if (keywords != null)
        {
            foreach (KeywordData keyword in keywords)
            {
                CreateKeywordButton(keyword);
            }
        }

        base.Show();
    }

    public void ShowCsv(string title, IEnumerable<CsvKeywordRecord> keywords, Action<CsvKeywordRecord> keywordSelected)
    {
        if (!HasRequiredReferences())
        {
            Debug.LogWarning("KeywordSelectionUI is missing scene references.");
            return;
        }

        BindCancelButton();
        ClearButtons();
        _keywordSelected = null;
        _csvKeywordSelected = keywordSelected;
        SetTitle(title);

        if (keywords != null)
        {
            foreach (CsvKeywordRecord keyword in keywords)
            {
                CreateCsvKeywordButton(keyword);
            }
        }

        base.Show();
    }

    public override void Hide()
    {
        ClearButtons();
        _keywordSelected = null;
        _csvKeywordSelected = null;
        SetTitle(string.Empty);
        base.Hide();
    }

    private void CreateKeywordButton(KeywordData keyword)
    {
        if (keyword == null)
        {
            return;
        }

        Button button = CreateButton(keyword.DisplayName);
        if (button == null)
        {
            return;
        }

        KeywordData capturedKeyword = keyword;
        button.onClick.AddListener(() => SelectKeyword(capturedKeyword));
    }

    private void CreateCsvKeywordButton(CsvKeywordRecord keyword)
    {
        if (keyword == null)
        {
            return;
        }

        Button button = CreateButton(keyword.DisplayName);
        if (button == null)
        {
            return;
        }

        CsvKeywordRecord capturedKeyword = keyword;
        button.onClick.AddListener(() => SelectCsvKeyword(capturedKeyword));
    }

    private Button CreateButton(string labelText)
    {
        if (keywordButtonPrefab == null || buttonContainer == null)
        {
            return null;
        }

        Button button = Instantiate(keywordButtonPrefab, buttonContainer);
        button.gameObject.SetActive(true);

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -_spawnedButtons.Count * 60f);
            rect.sizeDelta = new Vector2(360f, 48f);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = labelText;
        }

        _spawnedButtons.Add(button);
        return button;
    }

    private void SelectKeyword(KeywordData keyword)
    {
        Action<KeywordData> callback = _keywordSelected;
        Hide();
        callback?.Invoke(keyword);
    }

    private void SelectCsvKeyword(CsvKeywordRecord keyword)
    {
        Action<CsvKeywordRecord> callback = _csvKeywordSelected;
        Hide();
        callback?.Invoke(keyword);
    }

    private void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    private void ClearButtons()
    {
        foreach (Button button in _spawnedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        _spawnedButtons.Clear();
    }

    private bool HasRequiredReferences()
    {
        return panelRoot != null &&
               titleText != null &&
               buttonContainer != null &&
               keywordButtonPrefab != null &&
               cancelButton != null;
    }

    private void PrewarmTexts()
    {
        TmpTextPrewarmUtility.Prewarm(titleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(keywordButtonPrefab, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(cancelButton, TmpPrewarmText);
    }

    private void BindCancelButton()
    {
        if (_cancelBound || cancelButton == null)
        {
            return;
        }

        cancelButton.onClick.AddListener(Hide);
        _cancelBound = true;
    }

}
