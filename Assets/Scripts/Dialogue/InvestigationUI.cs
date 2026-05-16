using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// UI component that displays investigation results.
/// The panel can start inactive in the scene; Show activates it and Hide clears then deactivates it.
/// </summary>
public sealed class InvestigationUI : BasePanelUI
{
    private const string TmpPrewarmText = "\uAC00\uB098\uB2E4\uB77C\uB9C8\uBC14\uC0AC\uC544\uC790\uCC28\uCE74\uD0C0\uD30C\uD558 \uC54C\uB9AC\uBC14\uC774 \uC870\uC0AC \uB300\uD654 \uB3CC\uC544\uAC00\uAE30";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private DialogueLog dialogueLog;

    private readonly List<DialogueLine> _lines = new();
    private int _currentLineIndex;

    public bool HasNextLine => IsVisible && _currentLineIndex + 1 < _lines.Count;
    public int LastShownFrame { get; private set; } = -1;

    protected override void Awake()
    {
        base.Awake();

        TmpTextPrewarmUtility.Prewarm(titleText, TmpPrewarmText);
        TmpTextPrewarmUtility.Prewarm(bodyText, TmpPrewarmText);
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
        LastShownFrame = Time.frameCount;
        base.Show();
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

}

