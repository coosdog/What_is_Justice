using System.Text;
using TMPro;
using UnityEngine;

public sealed class EvidenceNotebookUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private EvidenceInventory evidenceInventory;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (evidenceInventory == null)
        {
            evidenceInventory = FindFirstObjectByType<EvidenceInventory>();
        }

        Hide();
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        Refresh();
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (bodyText == null)
        {
            return;
        }

        if (evidenceInventory == null)
        {
            bodyText.text = "획득한 단서가 없습니다.";
            return;
        }

        StringBuilder builder = new();
        builder.AppendLine("[단서]");
        foreach (EvidenceData evidence in evidenceInventory.Evidence)
        {
            builder.AppendLine($"- {evidence.DisplayName}: {evidence.Description}");
        }

        foreach (CsvEvidenceRecord evidence in evidenceInventory.CsvEvidence)
        {
            builder.AppendLine($"- {evidence.DisplayName}: {evidence.Description}");
        }

        builder.AppendLine();
        builder.AppendLine("[키워드]");
        foreach (KeywordData keyword in evidenceInventory.Keywords)
        {
            builder.AppendLine($"- {keyword.DisplayName}: {keyword.Description}");
        }

        foreach (CsvKeywordRecord keyword in evidenceInventory.CsvKeywords)
        {
            builder.AppendLine($"- {keyword.DisplayName}: {keyword.Description}");
        }

        bodyText.text = builder.ToString();
    }
}

