using System.Collections.Generic;
namespace SNCoreEditor.Input;


internal static class GameInputExtensions {
    extension(GameInput) {
        /// <summary>
        /// Returns whether the hotkey combination has been pressed that frame
        /// </summary>
        /// <param name="buttons">List of buttons to check. The last one in the list will be check as having been pressed down that frame. Consider any previous buttons as "modifiers" like ctrl or alt</param>
        /// <returns>whether the hotkey(s) has been pressed this frame</returns>
        internal static bool GetHotKeyComboDown(List<GameInput.Button> buttons) {
            for (int i = 0; i < buttons.Count; i++) {
                GameInput.Button button = buttons[i];

                //modifier
                if (i < buttons.Count - 1) {
                    if(!GameInput.GetButtonHeld(button)) return false;
                    continue;
                }
                //last hotkey
                if (!GameInput.GetButtonDown(button)) return false;
            }
            return true;
        }
    }
} 
