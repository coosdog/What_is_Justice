using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class PointerUiBlocker
{
    private static readonly List<RaycastResult> Results = new();

    public static bool IsBlocked(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        PointerEventData pointerData = new(eventSystem)
        {
            position = screenPosition
        };

        Results.Clear();
        eventSystem.RaycastAll(pointerData, Results);
        return Results.Count > 0;
    }
}
