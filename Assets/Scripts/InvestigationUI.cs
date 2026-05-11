using TMPro;
using UnityEngine;

/// <summary>
/// 조사 결과를 화면에 출력하는 전용 UI 컴포넌트.
/// 뷰 로직만 담당하여 다른 시스템과 결합도를 낮춥니다.
/// </summary>
public sealed class InvestigationUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Show(string title, string body)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (bodyText != null)
        {
            bodyText.text = body;
        }

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}