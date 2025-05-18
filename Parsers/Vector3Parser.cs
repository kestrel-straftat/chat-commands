using System;
using System.Globalization;
using UnityEngine;

namespace ChatCommands.Parsers;

public class Vector3Parser : ParserBase
{
    public override Type ParseResultType => typeof(Vector3);
    public override object Parse(string value) {
        Vector3 result = new();
        value = string.Join(' ', value.Split(new[] {',', '(', ')'}, StringSplitOptions.RemoveEmptyEntries));
        string[] xyz = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        result.x = float.Parse(xyz[0].Trim(), CultureInfo.CurrentCulture);
        result.y = float.Parse(xyz[1].Trim(), CultureInfo.CurrentCulture);
        result.z = float.Parse(xyz[2].Trim(), CultureInfo.CurrentCulture);
        return result;
    }
}