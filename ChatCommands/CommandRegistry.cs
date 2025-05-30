using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using ChatCommands.Attributes;

namespace ChatCommands;

public static class CommandRegistry
{
    // contains both command names and command aliases mapped to their respective commands
    internal static Dictionary<string, Command> Commands { get; private set; } = [];
    // category names from all currently registered commands (in lowercase)
    internal static HashSet<string> Categories { get; private set; } = [];

    /// <summary>
    /// Registers all command attributes from a specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to register commands from.</param>
    public static void RegisterCommandsFromAssembly(Assembly assembly) => PopulateFromAssembly(assembly);
    
    /// <summary>
    /// Registers all command attributes from the calling assembly.
    /// </summary>
    public static void RegisterCommandsFromAssembly() => PopulateFromAssembly(Assembly.GetCallingAssembly());

    /// <summary>
    /// Returns whether a command name/alias is registered.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <returns>True if the command exists, false otherwise.</returns>
    public static bool CommandExists(string alias) => Commands.ContainsKey(alias);
    
    /// <summary>
    /// Returns whether a category is registered.
    /// </summary>
    /// <param name="categoryName">The category name to check, not case sensitive.</param>
    /// <returns>True if the category exists, false otherwise.</returns>
    public static bool CategoryExists(string categoryName) => Categories.Contains(categoryName.ToLower());

    /// <summary>
    /// Gets the <see cref="Command"/> associated with the specified alias.
    /// </summary>
    /// <param name="alias">The alias of the command to get.</param>
    /// <param name="command">When this method returns, contains the command associated with the specified alias, if the alias is found; otherwise, null. This parameter is passed uninitialized.</param>
    /// <returns>true if a command with the specified alias exists; otherwise, false.</returns>
    public static bool TryGet(string alias, out Command command) => Commands.TryGetValue(alias, out command);
    
    // gets a list of commands which share names or any aliases with
    // the provided command.
    private static Command[] GetCollisions(Command command) {
        var collisions = new HashSet<Command>();
        foreach (var alias in command.aliases.Append(command.name)) {
            if (TryGet(alias, out var collision))
                collisions.Add(collision);
        }
        return collisions.ToArray();
    }
    
    private static void Add(Command command) {
        Categories.Add(command.categoryName.ToLower());
        Commands.Add(command.name, command);
        foreach (var alias in command.aliases) {
            Commands.Add(alias, command);
        }
    }

    private static void AddOverride(Command command) {
        Categories.Add(command.categoryName.ToLower());
        Commands[command.name] = command;
        foreach (var alias in command.aliases) {
            Commands[alias] = command;
        }
    }

    internal static void RegisterCommand(Command command) {
        var collisions = GetCollisions(command);
        if (collisions.Length == 0) {
            Add(command);
        }
        else {
            if (collisions.All(c => c.registryPriority < command.registryPriority)) {
                // unregister overriden commands
                foreach (var collision in collisions) {
                    foreach (var alias in collision.aliases.Append(collision.name))
                        // FIXME (?) this wont remove the category name. i dont think itll really be an issue though
                        Commands.Remove(alias);
                }
                command.isOverriden = true;
                AddOverride(command);
            } else if (collisions.Any(c => c.registryPriority == command.registryPriority)) {
                var assembly = command.method.DeclaringType?.Assembly ?? Assembly.GetCallingAssembly(); 
                LogConflictError(command.aliases.Prepend(command.name).ToArray(), command.registryPriority, assembly);
            }
        }
    }
    
    private static void PopulateFromAssembly(Assembly assembly) {
        Plugin.Logger.LogInfo($"Registering commands from \"{assembly.GetName().Name}\"");
        var methods = assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic))
            .Where(method => method.IsDefined(typeof(CommandAttribute), false));

        foreach (var method in methods) {
            var methodType = method.DeclaringType;
            var commandAttr = method.GetCustomAttribute<CommandAttribute>();
            bool cmdHasAliases = method.IsDefined(typeof(CommandAliasesAttribute), false);
            bool cmdIsOverride = method.IsDefined(typeof(CommandOverrideAttribute), false);
            bool cmdHasCategory = methodType?.IsDefined(typeof(CommandCategoryAttribute), false) ?? false;
            
            string category = cmdHasCategory ? methodType.GetCustomAttribute<CommandCategoryAttribute>()?.name : "Misc";
            int priority = cmdIsOverride ? method.GetCustomAttribute<CommandOverrideAttribute>().priority : 0;
            
            Command cmd;
            if (cmdHasAliases) {
                var aliasesAttr = method.GetCustomAttribute<CommandAliasesAttribute>();
                cmd = new Command(method, commandAttr.name, commandAttr.description, category, commandAttr.flags, priority, aliasesAttr.aliases);
            }
            else {
                cmd = new Command(method, commandAttr.name, commandAttr.description, category, commandAttr.flags, priority);
            }

            RegisterCommand(cmd);
        }
    }

    // oh my god if i get people asking me what is wrong with their mods after putting this thing in here im going to start breaking things
    private static void LogConflictError(string[] problematicCommandNames, int priorityAttempted, Assembly definingAssembly) {
        Plugin.Logger.LogError($"Error when registering command \"{problematicCommandNames[0]}\" from \"{definingAssembly.GetName().Name}\" w/ priority {priorityAttempted}:" +
                               $" a command with the same name or one of the same aliases is already registered with the same priority!");
        Plugin.Logger.LogError("Command conflicts:");
        
        foreach (var alias in problematicCommandNames) {
            if (!TryGet(alias, out var cmd)) continue;
            var builder = new StringBuilder($"\"{cmd.method.DeclaringType?.Assembly.GetName().Name ?? "UNKNOWN ASSEMBLY"}\" registers [");
            builder.Append("\"")
                .Append(cmd.name)
                .Append("\" w/ priority: ")
                .Append(cmd.registryPriority);
            if (cmd.aliases.Length > 0) {
                builder.Append(", aliases: ");
                foreach (var cmdAlias in cmd.aliases) {
                    builder.Append("\"")
                        .Append(cmdAlias)
                        .Append("\" ");
                }
            }

            builder.Append("\b]");
            
            Plugin.Logger.LogWarning(builder.ToString());
            builder.Clear();
        }
    }
    
    // yeah maybe lets not do that ever. keeping it here though
    [Obsolete("slow. bad idea.")]
    internal static void PopulateAll() {
        foreach (var assembly in Chainloader.PluginInfos.Values
                     .Select(plugin => plugin?.Instance?.GetType().Assembly)
                     .Where(assembly => assembly is not null)) {
            PopulateFromAssembly(assembly);
        }
    }
}