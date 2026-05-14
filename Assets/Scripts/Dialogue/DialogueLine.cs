public readonly struct DialogueLine
{
    public string Speaker { get; }
    public string Text { get; }

    public DialogueLine(string speaker, string text)
    {
        Speaker = speaker;
        Text = text;
    }
}
