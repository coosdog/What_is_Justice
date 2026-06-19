using System;

public sealed class SpeakerProfile
{
    public SpeakerProfile(string speakerKey, string displayName, string defaultPortraitKey, string speakerType)
    {
        SpeakerKey = Normalize(speakerKey);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? SpeakerKey : displayName.Trim();
        DefaultPortraitKey = Normalize(defaultPortraitKey);
        SpeakerType = Normalize(speakerType);
    }

    public string SpeakerKey { get; }
    public string DisplayName { get; }
    public string DefaultPortraitKey { get; }
    public string SpeakerType { get; }

    public bool Matches(string speakerKey)
    {
        return string.Equals(SpeakerKey, Normalize(speakerKey), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
