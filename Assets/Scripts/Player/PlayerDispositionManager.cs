using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerDispositionManager : PersistentSingleton<PlayerDispositionManager>
{
    [SerializeField] private PlayerDisposition currentDisposition = PlayerDisposition.Basic;
    [SerializeField] private bool allowKeyboardSwitch = true;

    public event Action<PlayerDisposition> DispositionChanged;

    public PlayerDisposition CurrentDisposition
    {
        get => currentDisposition;
        set
        {
            if (currentDisposition == value)
            {
                return;
            }

            currentDisposition = value;
            DispositionChanged?.Invoke(currentDisposition);
        }
    }

    public string DisplayName => GetDisplayName(currentDisposition);
    public bool AllowKeyboardSwitch { get => allowKeyboardSwitch; set => allowKeyboardSwitch = value; }

    private void Update()
    {
        if (!allowKeyboardSwitch)
        {
            return;
        }

        if (WasPressed(1))
        {
            CurrentDisposition = PlayerDisposition.Basic;
        }
        else if (WasPressed(2))
        {
            CurrentDisposition = PlayerDisposition.Tendency1;
        }
        else if (WasPressed(3))
        {
            CurrentDisposition = PlayerDisposition.Tendency2;
        }
        else if (WasPressed(4))
        {
            CurrentDisposition = PlayerDisposition.Tendency3;
        }
    }

    public void SetDisposition(PlayerDisposition disposition)
    {
        CurrentDisposition = disposition;
    }

    public string GetDisplayName()
    {
        return DisplayName;
    }

    public static bool IsInvestigationMode(PlayerDisposition disposition)
    {
        return disposition == PlayerDisposition.Basic ||
               disposition == PlayerDisposition.Tendency1;
    }

    public static bool IsInquiryMode(PlayerDisposition disposition)
    {
        return disposition == PlayerDisposition.Basic ||
               disposition == PlayerDisposition.Tendency2 ||
               disposition == PlayerDisposition.Tendency3;
    }

    public static string GetDisplayName(PlayerDisposition disposition)
    {
        return disposition switch
        {
            PlayerDisposition.Tendency1 => "\uC608\uB9AC\uD55C \uC2DC\uC57C",
            PlayerDisposition.Tendency2 => "\uBBF8\uC138\uD55C \uCCAD\uAC01",
            PlayerDisposition.Tendency3 => "\uCE68\uBB35\uC758 \uC751\uC2DC",
            _ => "\uAE30\uBCF8 \uAD00\uCC30"
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
            4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
            _ => false
        };
#elif ENABLE_LEGACY_INPUT_MANAGER
        return number switch
        {
            1 => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1),
            2 => Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2),
            3 => Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3),
            4 => Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4),
            _ => false
        };
#else
        return false;
#endif
    }
}
