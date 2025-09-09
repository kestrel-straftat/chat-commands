using System;

namespace ChatCommands.Parsing;

/// <summary>
/// Defines custom parsing behavior for a type, used by <see cref="ParserLocator"/>.
/// </summary>
public interface ITypeParseExtension
{
    /// <summary>The type to target (the result type of the parse operation).</summary>
    public Type Target { get; }
    public object Parse(string value);
}