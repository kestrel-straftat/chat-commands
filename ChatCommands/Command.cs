using System;
using System.Linq;
using System.Reflection;

namespace ChatCommands;

[Flags]
public enum CommandFlags
{
    None = 0,
    HostOnly = 1,           // Commands only the host of a lobby can run
    ExplorationOnly = 2,    // Commands that can only be run in map exploration mode
    IngameOnly = 4,         // Commands that can only be run when in a map
    Silent = 8,             // Commands whose output is not sent in the chat
    TryRunOnHost = 16,      // Commands which will be run on the host if possible
}

public class Command
{
    public readonly MethodInfo method;
    public readonly string description;
    public readonly CommandFlags flags;
    public readonly string name;
    public readonly string[] aliases;
    public readonly string categoryName;
    public readonly ParameterInfo[] parameterInfos;
    public readonly int maxParameters;
    public readonly int minParameters;
    public readonly int registryPriority;
    public readonly bool hasRequesterParameter;
    public bool isOverriden;

    public Command(MethodInfo method, string name, string description = "no description found", string categoryName = "Misc", CommandFlags flags = CommandFlags.None, int registryPriority = 0, params string[] aliases) {
        this.name = name;
        this.aliases = aliases;
        this.method = method;
        this.description = description;
        this.flags = flags;
        this.categoryName = categoryName;
        this.registryPriority = registryPriority;
        parameterInfos = method.GetParameters();
        maxParameters = parameterInfos.Length;
        minParameters = parameterInfos.Count(p => !p.HasDefaultValue);
        if (maxParameters > 0 && Evaluator.IsRequesterParameter(parameterInfos[^1])) {
            hasRequesterParameter = true;
            --maxParameters;
        }
    }
    
    public object Invoke(object[] args) => method.Invoke(null, args);
}