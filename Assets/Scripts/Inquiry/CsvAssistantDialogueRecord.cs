public sealed class CsvAssistantDialogueRecord
{
    public string AssistantDialogueId { get; }
    public AssistantDialogueTrigger TriggerType { get; }
    public string ConditionId { get; }
    public string Disposition { get; }
    public string[] ResponseDialogueIds { get; }
    public string FallbackText { get; }

    public CsvAssistantDialogueRecord(string assistantDialogueId, AssistantDialogueTrigger triggerType, string conditionId, string disposition, string[] responseDialogueIds, string fallbackText)
    {
        AssistantDialogueId = assistantDialogueId?.Trim();
        TriggerType = triggerType;
        ConditionId = conditionId?.Trim();
        Disposition = disposition?.Trim();
        ResponseDialogueIds = responseDialogueIds ?? System.Array.Empty<string>();
        FallbackText = fallbackText;
    }
}
