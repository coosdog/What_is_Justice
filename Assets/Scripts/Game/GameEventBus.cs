using System;

public static class GameEventBus
{
    public static event Action<string> Raised;

    public static void Raise(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        Raised?.Invoke(eventId.Trim());
    }
}
