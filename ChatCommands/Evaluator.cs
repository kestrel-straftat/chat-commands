using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ChatCommands.Parsers;
using FishNet;
using MyceliumNetworking;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;

namespace ChatCommands;

public class Evaluator
{
    public static Evaluator Instance => field ??= new Evaluator();

    public void Evaluate(string input) {
        var tokens = new CommandLexer(input).GetTokens();
        var commandParts = new List<string>();

        // horrible pseudo-pipe thing. works™ though
        object pipedResult = null;
        while (tokens.Count > 0) {
            var token = tokens.Dequeue();
            string commandName;
            switch (token.type) {
                case CommandLexer.TokenType.Seperator:
                    if (commandParts.Count == 0) break;
                    commandName = commandParts[0];
                    commandParts.RemoveAt(0);
                    // append piped result if possible
                    if (pipedResult is not null) {
                        commandParts.Add(pipedResult.ToString());
                        pipedResult = null;
                    }
                    
                    RunCommand(commandName, commandParts.ToArray());
                    commandParts.Clear();
                    break;
                
                case CommandLexer.TokenType.Pipe:
                    if (commandParts.Count == 0) break;
                    commandName = commandParts[0];
                    commandParts.RemoveAt(0);
                    if (pipedResult is not null) {
                        commandParts.Add(pipedResult.ToString());
                    }
                    
                    // store the result
                    pipedResult = RunCommand(commandName, commandParts.ToArray(), false);
                    commandParts.Clear();
                    break;
                
                case CommandLexer.TokenType.NestedString:
                case CommandLexer.TokenType.Identifier:
                default:
                    commandParts.Add(token.content);
                    break;
            }
        }
    }

    // a special parameter that will be filled with the steam ID of the user who requested
    // a command run on TryRunOnHost commands
    internal static bool IsRequesterParameter(ParameterInfo parameter) {
        return parameter.ParameterType == typeof(CSteamID) && parameter.Name == "requester";
    }

    private static bool TryParseParameters(Command cmd, CSteamID requester, in string[] stringArgs, out object[] parsedArgs, out string err) {
        var providedCount = stringArgs.Length;
        parsedArgs = new object[cmd.maxParameters];
        
        if (providedCount < cmd.minParameters) {
            err = $"Mismatched parameter counts: command \"{cmd.name}\" expects at least {cmd.minParameters}, but {providedCount} {(providedCount == 1 ? "was" : "were")} provided.";
            return false;
        }
        if (providedCount > cmd.maxParameters) {
            err = $"Mismatched parameter counts: command \"{cmd.name}\" expects at most {cmd.maxParameters}, but {providedCount} {(providedCount == 1 ? "was" : "were")} provided.";
            return false;
        }
        
        // attempt to convert and populate command params
        for (int i = 0; i < cmd.maxParameters; ++i) {
            if (i < providedCount) {
                var paramType = cmd.parameterInfos[i].ParameterType;
                try {
                    parsedArgs[i] = ParserLocator.ParseTo(paramType, stringArgs[i]);
                }
                catch {
                    err = $"Invalid parameter: command \"{cmd.name}\" expects the parameter \"{cmd.parameterInfos[i].Name}\" to be of type {paramType.Name}, but the provided parameter (\"{stringArgs[i]}\") could not be converted to it!";
                    return false;
                }
            }
            else {
                // append default values to call, if any exist
                parsedArgs[i] = cmd.parameterInfos[i].DefaultValue;
            }
            
            // set requester if present
            if (IsRequesterParameter(cmd.parameterInfos[i])) {
                parsedArgs[i] = requester;
            }
        }

        err = null;
        return true;
    }

    private object RunCommand(string name, string[] args, bool sendResult = true) {
        if (!CommandRegistry.TryGet(name, out var cmd)) {
            SystemMessage($"No command found with name \"{name}\".");
            return null;
        }
        
        if (cmd.flags.HasFlag(CommandFlags.IngameOnly) && PauseManager.Instance.inMainMenu) {
            SystemMessage($"The command \"{name}\" can only be run while in a game!");
            return null;
        }
        
        if (cmd.flags.HasFlag(CommandFlags.ExplorationOnly) && !SceneMotor.Instance.testMap) {
            SystemMessage($"The command \"{name}\" can only be run in exploration mode!");
            return null;
        }
        
        if (cmd.flags.HasFlag(CommandFlags.TryRunOnHost) && !InstanceFinder.IsHost) {
            if (!MyceliumNetwork.GetPlayerData<bool>(MyceliumNetwork.LobbyHost, "chatCommandsInstalled")) {
                SystemMessage($"The command \"{name}\" can only be run if the lobby host has chat commands installed!");
                return null;
            }
            
            // send command to host to run instead
            MyceliumNetwork.RPCTarget(Plugin.c_myceliumID, nameof(RPCRunCommand), MyceliumNetwork.LobbyHost, ReliableType.Reliable,
                name, JsonConvert.SerializeObject(args), sendResult
            );
            return null;
        }
        
        if (cmd.flags.HasFlag(CommandFlags.HostOnly) && !InstanceFinder.IsHost) {
            SystemMessage($"The command \"{name}\" can only be run as a lobby host!");
            return null;
        }

        if (!TryParseParameters(cmd, CSteamID.Nil, args, out var parsedArgs, out var err)) {
            SystemMessage(err);
            return null;
        }
        
        try {
            var result = cmd.Invoke(parsedArgs);
            if (result is not null && !cmd.flags.HasFlag(CommandFlags.Silent) && sendResult) 
                SystemMessage(result);
            return result;
        }
        catch (Exception e) {
            var inner = e.GetInnerMostException();
            if (inner is CommandException ce)
                SystemMessage($"Command \"{name}\" failed: {ce.Message}");
            else {
                SystemMessage($"Unknown error running command \"{name}\": {inner}");
            }
            return null;
        }
    }
    
    [CustomRPC]
    private void RPCRunCommand(string name, string jsonArgs, bool sendResult, RPCInfo info) {
        var args = JsonConvert.DeserializeObject<string[]>(jsonArgs);
        
        if (!CommandRegistry.TryGet(name, out var cmd)) {
            ReturnSystemMessage($"Host could not find a command with name \"{name}\". Do they have the same command mods as you?");
            return;
        }
        
        if (!TryParseParameters(cmd, info.SenderSteamID, args, out var parsedArgs, out var err)) {
            ReturnSystemMessage(err);
            return;
        }
        
        try {
            var result = cmd.Invoke(parsedArgs);
            if (result is not null && !cmd.flags.HasFlag(CommandFlags.Silent) && sendResult) {
                ReturnSystemMessage(result.ToString());
            }
        }
        catch (Exception e) {
            var inner = e.GetInnerMostException();
            if (inner is CommandException ce)
                ReturnSystemMessage($"Command \"{name}\" failed: {ce.Message}");
            else {
                ReturnSystemMessage($"Unknown error running command \"{name}\": {inner}");
            }
        }

        return;
        
        void ReturnSystemMessage(string message) {
            MyceliumNetwork.RPCTarget(Plugin.c_myceliumID, nameof(RPCSystemMessage), info.SenderSteamID, ReliableType.Reliable, message);
        }
    }

    [CustomRPC]
    private void RPCSystemMessage(string message) {
        SystemMessage(message);
    }
    
    // TEMPORARY; should clean up ChatPatches at some point and/or move this to another util mod
    private static void SystemMessage(object msg) => ChatPatches.SendSystemMessage(msg);
}

internal class CommandLexer(string source)
{
    public enum TokenType
    {
        Identifier,
        NestedString,
        Seperator,
        Pipe
    }

    public readonly struct Token(string content, TokenType type)
    {
        public readonly string content = content;
        public readonly TokenType type = type;
        
        public static Token Seperator => new(";", TokenType.Seperator);
        public static Token Pipe => new("|", TokenType.Pipe);
    }

    private int m_pos;
    private StringBuilder m_builder = new();
    
    private static bool IsSeperator(char c) => c is ';' or '\n';
    private static bool IsPipe(char c) => c is '|' or '>';
    private static bool IsQuote(char c) => c is '"' or '\'';

    private static bool IsIdentifierChar(char c) {
        return char.IsLetterOrDigit(c) || c is
            '_' or '-' or '.' or ':' or ',' or '(' or ')';
    }
    
    private static bool IsIgnorable(char c) {
        return !(IsSeperator(c) || IsQuote(c) || IsIdentifierChar(c) || IsPipe(c));
    }

    private static char GetEscaped(char c) => c switch {
        'n' => '\n',
        'r' => '\r',
        't' => '\t',
        'b' => '\b',
        _ => c // the other special ones have. limited use (looking at you \a)
    };
    
    private void SkipIgnorables() {
        while (m_pos < source.Length && IsIgnorable(source[m_pos])) {
            ++m_pos;
        }
    }
    
    private bool TryGetNextToken(out Token token) {
        SkipIgnorables();
        token = default;
        if (m_pos == source.Length)
            return false;

        var tokenType = TokenType.Identifier;
        char closingQuote = '\0';
        char current = source[m_pos];
        // Check if we're entering a nested string
        if (IsQuote(current)) {
            tokenType = TokenType.NestedString;
            closingQuote = current;
            ++m_pos;
        }
        else if (IsSeperator(current)) {
            ++m_pos;
            token = Token.Seperator;
            return true;
        } else if (IsPipe(current)) {
            ++m_pos;
            token = Token.Pipe;
            return true;
        }

        char c;
        for (; m_pos < source.Length; m_builder.Append(c), ++m_pos) {
            c = source[m_pos];
            // Should we be looking for a closing quote? if not the token
            // will end as soon as we stop seeing valid identifier chars.
            if (tokenType == TokenType.Identifier) {
                if (IsIdentifierChar(c))
                    continue;
            }
            else {
                // Append escaped chars raw
                if (c == '\\') {
                    if (m_pos + 1 < source.Length) {
                        c = GetEscaped(source[++m_pos]);
                        continue;
                    }
                }
                if (c != closingQuote)
                    continue;
                
                // Skip over closing quote
                ++m_pos;
            }
            
            break;
        }

        var tokenContent = m_builder.ToString();
        token = new Token(tokenContent, tokenType);
        m_builder.Clear();
        return true;
    }
    
    public Queue<Token> GetTokens() {
        var result = new Queue<Token>();
        while (TryGetNextToken(out var token)) {
            result.Enqueue(token);
        }
        result.Enqueue(Token.Seperator); // end of command break
        return result;
    }
}