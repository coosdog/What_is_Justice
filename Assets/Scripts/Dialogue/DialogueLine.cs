public readonly struct DialogueLine
{
    public string Speaker { get; }
    public string Text { get; }
    public string PortraitKey { get; }
    public string Emotion { get; }

    public DialogueLine(string speaker, string text, string portraitKey = "", string emotion = "")
    {
        Speaker = speaker;
        Text = text;
        PortraitKey = portraitKey;
        Emotion = emotion;
    }
}
