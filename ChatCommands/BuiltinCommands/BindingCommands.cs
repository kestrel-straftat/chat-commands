using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChatCommands.Attributes;
using ChatCommands.Parsers;
using UnityEngine;

namespace ChatCommands.BuiltinCommands;

[CommandCategory("Binding")]
public static class BindingCommands
{
    // F14 and F15 are used to serialize mwheelup and mwheeldown respectively
    // it's a bit hacky but keeps things reasonably clean
    private const KeyCode c_mwheelupKey = KeyCode.F14;
    private const KeyCode c_mwheeldownKey = KeyCode.F15;
    private const string c_mwheelup = "mwheelup";
    private const string c_mwheeldown = "mwheeldown";
    
    private static Dictionary<KeyCode, List<string>> m_binds;
    private static string m_savePath = Path.Combine(BepInEx.Paths.ConfigPath, PluginInfo.PLUGIN_GUID + ".binds.json");
    private static bool m_shouldIgnoreInput = false;
    
    public static void Init() {
        m_binds = Utils.LoadFromJsonFile<Dictionary<KeyCode, List<string>>>(m_savePath) ?? [];
    }

    public static void FixedUpdate() {
        m_shouldIgnoreInput = Utils.AnyInputFieldFocused();
    }
    
    public static void Update() {
        if (!Cursor.visible)
            switch (Input.mouseScrollDelta.y) {
                case > 0 when m_binds.TryGetValue(c_mwheelupKey, out var ucmds):
                    ucmds.ForEach(Evaluator.Evaluate);
                    break;
                case < 0 when m_binds.TryGetValue(c_mwheeldownKey, out var dcmds):
                    dcmds.ForEach(Evaluator.Evaluate);
                    break;
            }
        
        if (!Input.anyKeyDown || m_shouldIgnoreInput) return;
        
        foreach (var cmd in m_binds
                     .Where(bind => Input.GetKeyDown(bind.Key))
                     .SelectMany(bind => bind.Value)) {
            Evaluator.Evaluate(cmd);
        }
    }
    
    [Command("bind", "binds a command to a specified key")]
    public static string Bind(string key, string command) {
        var keyCode = ParseKey(key);
        
        if (m_binds.TryGetValue(keyCode, out var binds)) {
            if (binds.Contains(command))
                throw new CommandException("bind already exists");
            
            binds.Add(command);
        }
        else if (!m_binds.TryAdd(keyCode, [command]))
            throw new CommandException("unknown error while adding bind");
        
        Utils.SaveToJsonFile(m_binds, m_savePath);
        return $"bound {key.ToLower()} -> \"{command}\"";
    }

    [Command("unbind", "removes all bindings from the specified key")]
    public static string Unbind(string key) {
        var keyCode = ParseKey(key);
        
        m_binds.Remove(keyCode);
        Utils.SaveToJsonFile(m_binds, m_savePath);
        return $"unbound {key.ToLower()}";
    }
    
    private static KeyCode ParseKey(string key) {
        if (!ParserLocator.TryParseTo<KeyCode>(key, out var keyCode)
            && key != c_mwheelup && key != c_mwheeldown)
            throw new CommandException($"unknown key: \"{key}\"");
        
        return key.ToLower() switch {
            c_mwheelup => c_mwheelupKey,
            c_mwheeldown => c_mwheeldownKey,
            _ => keyCode
        };
    }

    [Command("unbindall", "removes all bindings")]
    public static string UnbindAll() {
        m_binds.Clear();
        Utils.SaveToJsonFile(m_binds, m_savePath);
        return "removed all bindings";
    }
    
    [Command("listbinds", "lists all bound keys")]
    [CommandAliases("key_listboundkeys")]
    public static string ListBinds() {
        var builder = new StringBuilder();
        foreach (var bind in m_binds) {
            var key = bind.Key switch {
                c_mwheelupKey => c_mwheelup,
                c_mwheeldownKey => c_mwheeldown,
                _ => bind.Key.ToString()
            };
            
            builder.Append($"\"{key}\" = ");
            for (int i = 0; i < bind.Value.Count; ++i) {
                builder.Append($"\"{bind.Value[i]}\"");
                if (i < bind.Value.Count - 1) 
                    builder.Append(", ");
            }
            builder.Append("\n");
        }
        var list = builder.ToString();
        return list == string.Empty ? "no binds" : list;
    }
}