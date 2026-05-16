using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InteractionChoiceUI : BasePanelUI
{
    private const string TmpPrewarmText = "\uBAA9\uACA9\uC790 \uB300\uD654\uD558\uAE30 \uC870\uC0AC\uD558\uAE30 \uB3CC\uC544\uAC00\uAE30";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button talkButton;
    [SerializeField] private Button inquiryButton;
    [SerializeField] private Button cancelButton;

    private Action _talkSelected;
    private Action _inquirySelected;
    private bool _buttonsBound;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
        PrewarmTexts();
        Hide();
    }

    public void Show(string title, Action talkSelected, Action inquirySelected)
    {
        if (!HasRequiredReferences())
        {
            Debug.LogWarning("InteractionChoiceUI is missing scene references.");
            return;
        }

        BindButtons();
        _talkSelected = talkSelected;
        _inquirySelected = inquirySelected;

        if (titleText != null)
        {
            titleText.text = title;
        }

        base.Show();
    }

    public override void Hide()
    {
        _talkSelected = null;
        _inquirySelected = null;
        base.Hide();
    }

    private void SelectTalk()
    {
        Action callback = _talkSelected;
        Hide();
        callback?.Invoke();
    }

    private void SelectInquiry()
    {
        Action callback = _inquirySelected;
        Hide();
        callback?.Invoke();
    }

    private bool HasRequiredReferences()
    {
        return panelRoot != null &&
               titleText != null &&
               talkButton != null &&
               inquiryButton != null &&
               cancelButton != null;
    }

    private void PrewarmTexts()
    {
        TmpTextPrewarmUtility.Prewarm(titleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(talkButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(inquiryButton, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(cancelButton, TmpPrewarmText);
    }

    private void BindButtons()
    {
        if (_buttonsBound || talkButton == null || inquiryButton == null || cancelButton == null)
        {
            return;
        }

        talkButton.onClick.AddListener(SelectTalk);
        inquiryButton.onClick.AddListener(SelectInquiry);
        cancelButton.onClick.AddListener(Hide);
        _buttonsBound = true;
    }

}
