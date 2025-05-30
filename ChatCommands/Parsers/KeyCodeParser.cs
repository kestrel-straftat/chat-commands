using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatCommands.Parsers;

internal class KeyCodeParser : ParserBase
{
    public override Type ParseResultType => typeof(KeyCode);
    public override object Parse(string value) {
        // this needs precedence~ otherwise numbers will parse incorrectly.
        if (m_alternativeStrings.TryGetValue(value.ToLower(), out var fallback))
            return fallback;
        if (Enum.TryParse(typeof(KeyCode), value, true, out var parsed))
            return parsed;
        
        throw new InvalidCastException();
    }

    private static Dictionary<string, KeyCode> m_alternativeStrings = new() {
        // numbers
        { "1", KeyCode.Keypad1 },
        { "2", KeyCode.Keypad2 },
        { "3", KeyCode.Keypad3 },
        { "4", KeyCode.Keypad4 },
        { "5", KeyCode.Keypad5 },
        { "6", KeyCode.Keypad6 },
        { "7", KeyCode.Keypad7 },
        { "8", KeyCode.Keypad8 },
        { "9", KeyCode.Keypad9 },
        { "0", KeyCode.Keypad0 },
        
        // other
        { "!", KeyCode.Exclaim },
        { "\"", KeyCode.DoubleQuote },
        { "#", KeyCode.Hash },
        { "$", KeyCode.Dollar },
        { "&", KeyCode.Ampersand },
        { "\'", KeyCode.Quote },
        { "(", KeyCode.LeftParen },
        { ")", KeyCode.RightParen },
        { "*", KeyCode.Asterisk },
        { "+", KeyCode.Plus },
        { ",", KeyCode.Comma },
        { "-", KeyCode.Minus },
        { ".", KeyCode.Period },
        { "/", KeyCode.Slash },
        { ":", KeyCode.Colon },
        { ";", KeyCode.Semicolon },
        { "<", KeyCode.Less },
        { "=", KeyCode.Equals },
        { ">", KeyCode.Greater },
        { "?", KeyCode.Question },
        { "@", KeyCode.At },
        { "[", KeyCode.LeftBracket },
        { "\\", KeyCode.Backslash },
        { "]", KeyCode.RightBracket },
        { "^", KeyCode.Caret },
        { "_", KeyCode.Underscore },
        { "`", KeyCode.BackQuote },
        
        // the source engine's mappings~ somewhat more intuitive than the unity ones at times so probably a good idea to include.
        // also might be more familiar to some people
        { "shift", KeyCode.LeftShift },
        { "rshift", KeyCode.RightShift },
        { "ctrl", KeyCode.LeftControl },
        { "rctrl", KeyCode.RightControl },
        { "alt", KeyCode.LeftAlt },
        { "ralt", KeyCode.RightAlt },
        { "enter", KeyCode.Return },
        { "lwin", KeyCode.LeftWindows },
        { "rwin", KeyCode.RightWindows },
        { "apps", KeyCode.Menu },
        { "ins", KeyCode.Insert },
        { "del", KeyCode.Delete },
        { "pgdn", KeyCode.PageDown },
        { "pgup", KeyCode.PageUp },
        
        { "kp_end", KeyCode.Keypad1 },
        { "kp_downarrow", KeyCode.Keypad2 },
        { "kp_pgdn", KeyCode.Keypad3 },
        { "kp_leftarrow", KeyCode.Keypad4 },
        { "kp_5", KeyCode.Keypad5 },
        { "kp_rightarrow", KeyCode.Keypad6 },
        { "kp_home", KeyCode.Keypad7 },
        { "kp_uparrow", KeyCode.Keypad8 },
        { "kp_pgup", KeyCode.Keypad9 },
        { "kp_enter", KeyCode.KeypadEnter },
        { "kp_ins", KeyCode.Keypad0 },
        { "kp_del", KeyCode.KeypadPeriod },
        { "kp_slash", KeyCode.KeypadDivide },
        { "kp_multiply", KeyCode.KeypadMultiply },
        { "kp_minus", KeyCode.KeypadMinus },
        { "kp_plus", KeyCode.KeypadPlus },
    };
}