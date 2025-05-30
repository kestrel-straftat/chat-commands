using System;

namespace ChatCommands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string name, string description, CommandFlags flags = CommandFlags.None) : Attribute
{
    public readonly string name = name;
    public readonly string description = description;
    public readonly CommandFlags flags = flags;
}