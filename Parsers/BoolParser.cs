using System;

namespace ChatCommands.Parsers;

internal class BoolParser : ParserBase
{
    public override Type ParseResultType => typeof(bool);
    public override object Parse(string value) {
        if (bool.TryParse(value, out var parsed))
            return parsed;

        if (int.TryParse(value, out var fallback))
            return fallback > 0;

        throw new InvalidCastException();
    }
}