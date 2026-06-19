public readonly struct MantraInferenceResult
{
    public MantraInferenceResult(bool success, string resultId, string title, string description)
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

    public static MantraInferenceResult Match(MantraRecipe recipe)
    {
        return new MantraInferenceResult(true, recipe.ResultId, recipe.DisplayName, recipe.Description);
    }

    public static MantraInferenceResult Failed(string message)
    {
        return new MantraInferenceResult(false, string.Empty, "불완전한 만트라", message);
    }
}
