using UnityEngine;

/// <summary>
/// 조사 가능한 오브젝트의 정적 데이터.
/// ScriptableObject로 분리하여 디자이너 친화적으로 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "ClueData", menuName = "Game/Investigation/Clue Data")]
public sealed class ClueData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string clueId;
    [SerializeField] private string displayName;

    [Header("Texts")]
    [TextArea(2, 5)]
    [SerializeField] private string firstInvestigationText;

    [TextArea(2, 5)]
    [SerializeField] private string alreadyInvestigatedText = "이미 조사한 대상이다.";

    [Header("Reward")]
    [SerializeField] private bool grantsClueItem;

    public string ClueId => clueId;
    public string DisplayName => displayName;
    public string FirstInvestigationText => firstInvestigationText;
    public string AlreadyInvestigatedText => alreadyInvestigatedText;
    public bool GrantsClueItem => grantsClueItem;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 세이브 키/식별자 누락 방지용 기본값 자동 보정
        if (string.IsNullOrWhiteSpace(clueId))
        {
            clueId = name;
        }
    }
#endif
}