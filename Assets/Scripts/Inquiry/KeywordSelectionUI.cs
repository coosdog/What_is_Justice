using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class KeywordSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button keywordButtonPrefab;
    [SerializeField] private Button cancelButton;

    private readonly List<Button> _spawnedButtons = new();
    private Action<KeywordData> _keywordSelected;
    private Action<CsvKeywordRecord> _csvKeywordSelected;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    public void Show(string title, IEnumerable<KeywordData> keywords, Action<KeywordData> keywordSelected)
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

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

        panelRoot.SetActive(true);
    }

    public void ShowCsv(string title, IEnumerable<CsvKeywordRecord> keywords, Action<CsvKeywordRecord> keywordSelected)
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

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

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        ClearButtons();
        _keywordSelected = null;
        _csvKeywordSelected = null;
        SetTitle(string.Empty);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
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
}
