using System;

namespace ChatCommands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAliasesAttribute(params string[] aliases) : Attribute
{
    public readonly string[] aliases = aliases;
}