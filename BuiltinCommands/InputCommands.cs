using ChatCommands.Attributes;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace ChatCommands.BuiltinCommands;

[CommandCategory("Input")]
public static class InputCommands
{
    [Command("jump", "triggers a jump")]
    public static void Jump() {
        PauseManager.Instance.chatting = false;
        Settings.Instance.localPlayer.Jump(new InputAction.CallbackContext());
    }
    
    private static int m_mouseDownFrames;
    private static ButtonControl m_buttonControl;
    
    [Command("click", "triggers a click with the specified mouse button, left by default")]
    public static void Click(int button = 1) {
        m_buttonControl = button switch {
            1 => Mouse.current.leftButton,
            2 => Mouse.current.rightButton,
            3 => Mouse.current.middleButton,
            4 => Mouse.current.forwardButton,
            5 => Mouse.current.backButton,
            _ => throw new CommandException($"invalid mouse button: {button}")
        };
        m_mouseDownFrames = 0;
        InputSystem.onBeforeUpdate += QueueMDown;
    }
    
    // like 2 hours of input system themed hell have culmnated in this. i hope its fine ish
    private static void QueueMDown() {
        using (StateEvent.From(Mouse.current, out var ptr)) {
            m_buttonControl.WriteValueIntoEvent<float>(1-m_mouseDownFrames, ptr);
            InputSystem.QueueEvent(ptr);
        }
        if (++m_mouseDownFrames > 1) InputSystem.onBeforeUpdate -= QueueMDown;
    }


}