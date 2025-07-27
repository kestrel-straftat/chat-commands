using System;
using System.Linq;
using System.Text;
using ChatCommands.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ChatCommands.BuiltinCommands;

[CommandCategory("Utility")]
public static class UtilityCommands
{
    [Command("echo", "outputs its input")]
    public static string Echo(string input) => input;

    [Command("eval", "evaluates its input as a command")]
    public static void Eval(string input) => Evaluator.Evaluate(input);

    [Command("repeat", "executes a command a specified number of times")]
    public static void Repeat(string command, int n) {
        for (int i = 0; i < n; ++i) {
            Evaluator.Evaluate(command);
        }
    }
    
    [Command("concat", "concatenates two strings")]
    public static string Concat(string first, string second) => string.Concat(first, second);
    
    [Command("add", "adds the provided numbers")]
    public static float Add(float first, float second) => first + second;
    
    [Command("subtract", "subtracts the second number from the first")]
    public static float Subtract(float first, float second) => first - second;
    
    [Command("rsubtract", "subtracts the first number from the second")]
    public static float RSubtract(float first, float second) => second - first;
    
    [Command("multiply", "multiplies the provided numbers")]
    public static float Multiply(float first, float second) => first * second;
    
    [Command("divide", "divides the first number by the second")]
    public static float Divide(float first, float second) => first / second;

    [Command("rdivide", "divides the second number by the first")]
    public static float RDivide(float first, float second) => second / first;
    
    [Command("randint", "gets a random int in [min, max) (min inclusive, max exclusive)")]
    public static int RandInt(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    
    [Command("randfloat", "gets a random float in [min, max] (inclusive)")]
    public static float RandFloat(float minInclusive, float maxInclusive) => Random.Range(minInclusive, maxInclusive);
    
    [Command("clear", "clears the chat")]
    [CommandAliases("cls")]
    public static void Clear() => ChatPatches.ClearChat();

    [Command("say", "sends a message in chat")]
    public static void Say(string message) => ChatPatches.SendChatMessage(FilterSystem.FilterString(message));
    
    private static CommandFlags[] m_allFlags = (CommandFlags[])Enum.GetValues(typeof(CommandFlags));
    
    [Command("help", "prints command info")]
    [CommandAliases("h")]
    public static string Help(string commandOrCategoryName = "") {
        var builder = new StringBuilder();

        if (commandOrCategoryName != string.Empty) {
            if (CommandRegistry.TryGet(commandOrCategoryName, out var cmd)) {
                builder.AppendHelpTextForCommand(cmd);
            }
            else {
                if (!CommandRegistry.CategoryExists(commandOrCategoryName))
                    throw new CommandException($"No command or category with the name {commandOrCategoryName} found.");

                // ok, try searching for commands with that category name
                var commandsInCategory = CommandRegistry.Commands.Values
                    .Where(c => string.Equals(c.categoryName, commandOrCategoryName, StringComparison.InvariantCultureIgnoreCase))
                    .Distinct()
                    .ToArray();
                
                if (commandsInCategory.Length == 0)
                    throw new CommandException($"No command or category with the name {commandOrCategoryName} found.");

                builder.AppendLine($"<u><size=150%><#B4F6B7>{commandsInCategory[0].categoryName}</color></size></u>")
                    .AppendLine();
                foreach (var command in commandsInCategory.OrderBy(c => c.name)) {
                    builder.AppendHelpTextForCommand(command);
                    builder.AppendLine().AppendLine();
                }
            }
        }
        // show help for all commands
        else {
            string currentCategory = "";
            // Distinct() needed to ignore aliases as they're kept in the same array as the name
            foreach (var command in CommandRegistry.Commands.Values.OrderBy(c => c.categoryName).ThenBy(c => c.name).Distinct()) {

                if (command.categoryName != currentCategory) {
                    currentCategory = command.categoryName;
                    builder.AppendLine($"<u><size=150%><#B4F6B7>{command.categoryName}</color></size></u>");
                    builder.AppendLine();
                }

                builder.AppendHelpTextForCommand(command);
                
                builder.AppendLine().AppendLine();
            }
        }
        return builder.ToString();
    }

    private static void AppendHelpTextForCommand(this StringBuilder builder, Command command) {
        if (command.isOverriden)
            builder.Append(SmallText("(overriden) "));
        builder.AppendLine($"<u>{command.name}</u>");
        builder.AppendLine(command.description);

        builder.Append("aliases: [");
        if (command.aliases.Length == 0) builder.Append("none");
        for (var i = 0; i < command.aliases.Length; ++i) {
            builder.Append(command.aliases[i]);
            if (i < command.aliases.Length - 1) builder.Append(", ");
        }
        builder.AppendLine("]");
        
        builder.Append("parameters: [");
        if (command.maxParameters == 0) {
            builder.Append("none");
        }
        else {
            for (var i = 0; i < command.maxParameters; ++i) {
                var param = command.parameterInfos[i];
                if (param.HasDefaultValue)
                    builder.Append(SmallText("(optional) "));
                builder.Append(param.Name)
                    .Append(": ")
                    .Append(param.ParameterType.Name.ToLower());
                if (i < command.parameterInfos.Length - 1) builder.Append(", ");
            }
        }
        builder.AppendLine("]");

        if (command.flags != CommandFlags.None) {
            builder.Append("flags: [");
            for (var i = 0; i < m_allFlags.Length; ++i) {
                if (m_allFlags[i] == 0 || !command.flags.HasFlag(m_allFlags[i])) continue;
                builder.Append(m_allFlags[i]);
                if (i < m_allFlags.Length - 1) builder.Append(", ");
            }
            builder.AppendLine("]");
        }

        builder.Append("returns: ")
            .Append(command.method.ReturnType == typeof(void) ? "none" : command.method.ReturnType.Name.ToLower());
    }
    
    private static string SmallText(string text) {
        return $"<alpha=#AA><voffset=0.1em><size=65%>{text}</size></voffset><alpha=#FF>";
    }
}

[CommandCategory("Settings")]
public static class SettingsCommands
{
    // Pseudo-convars. i think this is a (relatively) nice pattern to follow that means i don't have to actually implement convars. :3
    // the min value of the numeric type used represents a get and is the default parameter. its a weird way of doing it but it works well enough

    private static float PseudoConvarFloat(float value, Func<float> getter, Func<float, float> setter) {
        return value != float.MinValue ? setter(value) : getter();
    }
    
    private static int PseudoConvarInt(int value, Func<int> getter, Func<int, int> setter) {
        return value != int.MinValue ? setter(value) : getter();
    }
    
    // kiiinda slow but id rather have ease of use here
    private static float PseudoConvarSetting(float value, string settingName) {
        var field = typeof(Settings).GetField(settingName);
        var instance = Settings.Instance;
        if (value == float.MinValue)
            return (float)field.GetValue(instance);
        
        field.SetValue(instance, value);
        PlayerPrefs.SetFloat(settingName, value);
        PlayerPrefs.Save();
        return value;
    }
    
    [Command("maxfps", "changes max fps", CommandFlags.Silent)]
    public static int MaxFps(int fps = int.MinValue)
        => PseudoConvarInt(fps, () => Application.targetFrameRate, f => Application.targetFrameRate = f);

    [Command("fov", "changes fov", CommandFlags.Silent)]
    public static float Fov(float fov = float.MinValue)
        => PseudoConvarSetting(fov, "fovValue");

    [Command("gamma", "changes gamma (brightness)", CommandFlags.Silent)]
    public static float Gamma(float gamma = float.MinValue)
        => PseudoConvarSetting(gamma, "brightness");
    
    [Command("sensitivity", "changes sensitivity", CommandFlags.Silent)]
    public static float Sensitivity(float sensitivity = float.MinValue)
        => PseudoConvarSetting(sensitivity, "mouseSensitivity");
    
    [Command("aimsensitivity", "changes aiming sensitivity", CommandFlags.Silent)]
    public static float AimSensitivity(float sensitivity = float.MinValue)
        => PseudoConvarSetting(sensitivity, "mouseAimSensitivity");
    
    [Command("scopesensitivity", "changes scope sensitivity", CommandFlags.Silent)]
    public static float ScopeSensitivity(float sensitivity = float.MinValue)
        => PseudoConvarSetting(sensitivity, "mouseAimScopeSensitivity");
}