using System;
using System.Globalization;
using UnityEngine;

namespace ChatCommands.Parsing;

public class Vector3Parser : ITypeParseExtension
{
    public Type Target => typeof(Vector3);
    public object Parse(string value) {
        var result = new Vector3();
        value = string.Join(' ', value.Split([',', '(', ')'], StringSplitOptions.RemoveEmptyEntries));
        string[] xyz = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        result.x = float.Parse(xyz[0].Trim(), CultureInfo.CurrentCulture);
        result.y = float.Parse(xyz[1].Trim(), CultureInfo.CurrentCulture);
        result.z = float.Parse(xyz[2].Trim(), CultureInfo.CurrentCulture);
        return result;
    }
}