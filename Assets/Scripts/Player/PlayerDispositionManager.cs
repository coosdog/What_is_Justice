using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerDispositionManager : MonoBehaviour
{
    [SerializeField] private PlayerDisposition currentDisposition = PlayerDisposition.Basic;
    [SerializeField] private bool allowKeyboardSwitch = true;

    public event Action<PlayerDisposition> DispositionChanged;

    public PlayerDisposition CurrentDisposition => currentDisposition;

    private void Update()
    {
        if (!allowKeyboardSwitch)
        {
            return;
        }

        if (WasPressed(1))
        {
            SetDisposition(PlayerDisposition.Basic);
        }
        else if (WasPressed(2))
        {
            SetDisposition(PlayerDisposition.Tendency1);
        }
        else if (WasPressed(3))
        {
            SetDisposition(PlayerDisposition.Tendency2);
        }
    }

    public void SetDisposition(PlayerDisposition disposition)
    {
        if (currentDisposition == disposition)
        {
            return;
        }

        currentDisposition = disposition;
        DispositionChanged?.Invoke(currentDisposition);
    }

    public string GetDisplayName()
    {
        return currentDisposition switch
        {
            PlayerDisposition.Tendency1 => "성향 1",
            PlayerDisposition.Tendency2 => "성향 2",
            _ => "기본"
        };
    }

    private static bool WasPressed(int number)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return number switch
        {
            1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
            2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
            3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
            _ => false
        };
#elif ENABLE_LEGACY_INPUT_MANAGER
        return number switch
        {
            1 => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1),
            2 => Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2),
            3 => Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3),
            _ => false
        };
#else
        return false;
#endif
    }
}
