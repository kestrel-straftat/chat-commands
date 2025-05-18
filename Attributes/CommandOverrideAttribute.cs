using System;

namespace ChatCommands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandOverrideAttribute(int priority = int.MaxValue) : Attribute
{
    public readonly int priority = priority;
}