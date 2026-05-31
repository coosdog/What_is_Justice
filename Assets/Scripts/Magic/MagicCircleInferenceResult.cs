public readonly struct MagicCircleInferenceResult
{
    public MagicCircleInferenceResult(bool success, string resultId, string title, string description)
    {
        Success = success;
        ResultId = resultId;
        Title = title;
        Description = description;
    }

    public bool Success { get; }
    public string ResultId { get; }
    public string Title { get; }
    public string Description { get; }

    public static MagicCircleInferenceResult Match(MagicCircleRecipe recipe)
    {
        return new MagicCircleInferenceResult(true, recipe.ResultId, recipe.DisplayName, recipe.Description);
    }

    public static MagicCircleInferenceResult Failed(string message)
    {
        return new MagicCircleInferenceResult(false, string.Empty, "\uBD88\uC644\uC804\uD55C \uB9C8\uBC95\uC9C4", message);
    }
}
