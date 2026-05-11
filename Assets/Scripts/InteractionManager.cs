using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조사 상태 및 조사 결과 이벤트를 관리하는 핵심 매니저.
/// 싱글톤 없이 씬 참조로 동작하며, 세이브 시스템과 연결 가능한 형태로 설계했습니다.
/// </summary>
public sealed class InteractionManager : MonoBehaviour
{
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private InteractableObject[] interactables;

    /// <summary>
    /// 조사 완료 시 외부 시스템(대화/인벤토리/노트)에서 구독 가능한 이벤트.
    /// bool: 이번 조사에서 최초 조사인지 여부
    /// bool: 단서 획득 여부
    /// </summary>
    public event Action<ClueData, bool, bool> InvestigationCompleted;

    private readonly HashSet<string> _investigatedClueIds = new();

    private void Awake()
    {
        // 수동 할당이 없으면 자동 수집 (씬 초기화 1회만 수행)
        if (interactables == null || interactables.Length == 0)
        {
            interactables = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        }

        BindInteractables(true);
    }

    private void OnDestroy()
    {
        BindInteractables(false);
    }

    private void BindInteractables(bool bind)
    {
        if (interactables == null)
        {
            return;
        }

        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null)
            {
                continue;
            }

            if (bind)
            {
                interactable.Clicked += HandleInteractableClicked;
            }
            else
            {
                interactable.Clicked -= HandleInteractableClicked;
            }
        }
    }

    private void HandleInteractableClicked(InteractableObject interactable)
    {
        ClueData data = interactable != null ? interactable.Data : null;
        if (data == null)
        {
            return;
        }

        bool isFirstInvestigation = _investigatedClueIds.Add(data.ClueId);
        bool grantedClue = false;
        string body;

        if (isFirstInvestigation)
        {
            body = data.FirstInvestigationText;
            grantedClue = data.GrantsClueItem;
        }
        else
        {
            body = data.AlreadyInvestigatedText;
        }

        if (investigationUI != null)
        {
            investigationUI.Show(data.DisplayName, body);
        }

        InvestigationCompleted?.Invoke(data, isFirstInvestigation, grantedClue);
    }

    // ===== 저장/로드 연동용 API =====
    public IReadOnlyCollection<string> GetInvestigatedIds() => _investigatedClueIds;

    public void RestoreInvestigatedIds(IEnumerable<string> investigatedIds)
    {
        _investigatedClueIds.Clear();
        if (investigatedIds == null)
        {
            return;
        }

        foreach (string id in investigatedIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _investigatedClueIds.Add(id);
            }
        }
    }
}