using System;

[Serializable]
public sealed class CsvNpcInteractionRuleRecord
{
    public string RuleId { get; }
    public string NpcId { get; }
    public string Action { get; }
    public int Priority { get; }
    public string ConditionType { get; }
    public string ConditionValue { get; }
    public string ResultType { get; }
    public string ResultValue { get; }
    public bool RunOnce { get; }

    public CsvNpcInteractionRuleRecord(
        string ruleId,
        string npcId,
        string action,
        int priority,
        string conditionType,
        string conditionValue,
        string resultType,
        string resultValue,
        bool runOnce)
    {
        RuleId = ruleId?.Trim();
        NpcId = npcId?.Trim();
        Action = action?.Trim();
        Priority = priority;
        ConditionType = conditionType?.Trim();
        ConditionValue = conditionValue?.Trim();
        ResultType = resultType?.Trim();
        ResultValue = resultValue?.Trim();
        RunOnce = runOnce;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(RuleId) &&
        !string.IsNullOrWhiteSpace(NpcId) &&
        !string.IsNullOrWhiteSpace(Action) &&
        !string.IsNullOrWhiteSpace(ConditionType) &&
        !string.IsNullOrWhiteSpace(ResultType) &&
        !string.IsNullOrWhiteSpace(ResultValue);
}
