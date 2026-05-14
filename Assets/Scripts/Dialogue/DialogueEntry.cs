using System;

[Serializable]
public sealed class DialogueEntry
{
    public string Id { get; }
    public string Speaker { get; }
    public string Text { get; }
    public string PortraitKey { get; }
    public string Emotion { get; }

    public DialogueEntry(string id, string speaker, string text, string portraitKey, string emotion)
    {
        Id = id;
        Speaker = speaker;
        Text = text;
        PortraitKey = portraitKey;
        Emotion = emotion;
    }
}
