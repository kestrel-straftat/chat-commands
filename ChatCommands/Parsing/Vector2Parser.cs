using System;
using System.Globalization;
using UnityEngine;

namespace ChatCommands.Parsing;

public class Vector2Parser : ITypeParseExtension
{
    public Type Target => typeof(Vector2);
    public object Parse(string value) {
        var result = new Vector2();
        value = string.Join(' ', value.Split([',', '(', ')'], StringSplitOptions.RemoveEmptyEntries));
        string[] xy = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        result.x = float.Parse(xy[0].Trim(), CultureInfo.CurrentCulture);
        result.y = float.Parse(xy[1].Trim(), CultureInfo.CurrentCulture);
        return result;
    }
}