using System;

namespace ChatCommands.Parsers;

public abstract class ParserBase
{
    public abstract Type ParseResultType { get; }
    public abstract object Parse(string value);
}