using System;

[Serializable]
public sealed class DialogueEntry
{
    public string Id { get; }
    public string Speaker { get; }
    public string Text { get; }
    public string PortraitKey { get; }
    public string Emotion { get; }
    public string BoardNodeId { get; }
    public string BoardDisplayName { get; }
    public string BoardDescription { get; }
    public bool ShowPortraits { get; }
    public string PortraitLayout { get; }

    public DialogueEntry(
        string id,
        string speaker,
        string text,
        string portraitKey,
        string emotion,
        string boardNodeId,
        string boardDisplayName,
        string boardDescription,
        bool showPortraits,
        string portraitLayout)
    {
        Id = id;
        Speaker = speaker;
        Text = text;
        PortraitKey = portraitKey;
        Emotion = emotion;
        BoardNodeId = boardNodeId;
        BoardDisplayName = boardDisplayName;
        BoardDescription = boardDescription;
        ShowPortraits = showPortraits;
        PortraitLayout = portraitLayout;
    }
}
