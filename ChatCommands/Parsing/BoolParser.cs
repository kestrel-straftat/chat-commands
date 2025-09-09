using System;

namespace ChatCommands.Parsing;

internal class BoolParser : ITypeParseExtension
{
    public Type Target => typeof(bool);
    public object Parse(string value) {
        if (bool.TryParse(value, out var parsed))
            return parsed;

        if (int.TryParse(value, out var fallback))
            return fallback > 0;

        throw new InvalidCastException();
    }
}