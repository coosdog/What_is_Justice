using System;
using UnityEngine;

/// <summary>
/// 씬에 배치되는 조사 가능 오브젝트 컴포넌트.
/// 마우스 오버 강조 이벤트와 조사 요청 이벤트를 제공합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class InteractableObject : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ClueData clueData;

    [Header("Highlight")]
    [Tooltip("강조 표시를 위한 스프라이트 렌더러(없으면 현재 오브젝트의 SpriteRenderer 사용)")]
    [SerializeField] private SpriteRenderer highlightTarget;

    [Tooltip("하이라이트 색상")]
    [SerializeField] private Color hoverColor = Color.yellow;

    [Tooltip("기본 색상으로 복귀할 시간(0이면 즉시)")]
    [SerializeField] private float colorLerpDuration = 0.08f;

    public event Action<InteractableObject> Clicked;

    private Color _defaultColor;
    private bool _hasHighlight;
    private bool _isHovered;

    public ClueData Data => clueData;

    private void Awake()
    {
        if (highlightTarget == null)
        {
            highlightTarget = GetComponent<SpriteRenderer>();
        }

        _hasHighlight = highlightTarget != null;
        if (_hasHighlight)
        {
            _defaultColor = highlightTarget.color;
        }
    }

    private void OnMouseEnter()
    {
        _isHovered = true;
        ApplyHighlight(true);
    }

    private void OnMouseExit()
    {
        _isHovered = false;
        ApplyHighlight(false);
    }

    private void OnMouseDown()
    {
        // UI 위 클릭 무시가 필요하면 EventSystem 연동 확장 가능
        Clicked?.Invoke(this);
    }

    private void ApplyHighlight(bool enable)
    {
        if (!_hasHighlight)
        {
            return;
        }

        // Update 루프 없이 즉시/짧은 보간으로 처리
        if (colorLerpDuration <= 0f)
        {
            highlightTarget.color = enable ? hoverColor : _defaultColor;
            return;
        }

        StopAllCoroutines();
        StartCoroutine(LerpColor(enable ? hoverColor : _defaultColor, colorLerpDuration));
    }

    private System.Collections.IEnumerator LerpColor(Color targetColor, float duration)
    {
        Color start = highlightTarget.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            highlightTarget.color = Color.Lerp(start, targetColor, t);
            yield return null;
        }

        highlightTarget.color = targetColor;

        // 상태 역전 시 안전하게 최종 상태 보정
        if (_isHovered && targetColor != hoverColor)
        {
            highlightTarget.color = hoverColor;
        }
        else if (!_isHovered && targetColor != _defaultColor)
        {
            highlightTarget.color = _defaultColor;
        }
    }
}