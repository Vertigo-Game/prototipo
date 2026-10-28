using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vertigo.UI
{
    // Navegação dos menus: W/S ou setas, Enter/Espaço, direcional/analógico e A/Start no controle.
    public static class MenuInput
    {
        public static bool UpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null &&
                    (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)) ||
                   (Gamepad.current != null &&
                    (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        public static bool DownPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null &&
                    (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)) ||
                   (Gamepad.current != null &&
                    (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
#endif
        }

        public static bool ConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null &&
                    (Keyboard.current.enterKey.wasPressedThisFrame ||
                     Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                     Keyboard.current.spaceKey.wasPressedThisFrame)) ||
                   (Gamepad.current != null &&
                    (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                   Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
