using System;

namespace ChatCommands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CommandCategoryAttribute(string name) : Attribute
{
    public readonly string name = name;
}