using System;
using System.Globalization;
using UnityEngine;

namespace ChatCommands.Parsers;

public class Vector2Parser : ParserBase
{
    public override Type ParseResultType => typeof(Vector2);
    public override object Parse(string value) {
        var result = new Vector2();
        value = string.Join(' ', value.Split([',', '(', ')'], StringSplitOptions.RemoveEmptyEntries));
        string[] xy = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        result.x = float.Parse(xy[0].Trim(), CultureInfo.CurrentCulture);
        result.y = float.Parse(xy[1].Trim(), CultureInfo.CurrentCulture);
        return result;
    }
}