using System;
using System.Collections.Generic;
using System.Text;
using ChatCommands.Parsers;
using FishNet;

namespace ChatCommands;

public static class Evaluator
{
    public static void Evaluate(string input) {
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

    public static object RunCommand(string name, string[] args, bool sendResult = true) {
        if (!CommandRegistry.TryGet(name, out var cmd)) {
            SystemMessage($"No command found with name \"{name}\".");
            return null;
        }
        
        if (cmd.flags.HasFlag(CommandFlags.IngameOnly) && PauseManager.Instance.inMainMenu) {
            SystemMessage($"The command \"{name}\" can only be run while in a game!");
            return null;
        }
        
        if (cmd.flags.HasFlag(CommandFlags.HostOnly) && !InstanceFinder.IsHost) {
            SystemMessage($"The command \"{name}\" can only be run as a lobby host!");
        }
        
        if (cmd.flags.HasFlag(CommandFlags.ExplorationOnly) && !SceneMotor.Instance.testMap) {
            SystemMessage($"The command \"{name}\" can only be run in exploration mode!");
            return null;
        }
        
        var providedCount = args.Length;
        if (providedCount < cmd.minParameters) {
            SystemMessage($"Mismatched parameter counts: command \"{name}\" expects at least {cmd.minParameters}, but {providedCount} {(providedCount == 1 ? "was" : "were")} provided.");
            return null;
        }
        if (providedCount > cmd.maxParameters) {
            SystemMessage($"Mismatched parameter counts: command \"{name}\" expects at most {cmd.maxParameters}, but {providedCount} {(providedCount == 1 ? "was" : "were")} provided.");
            return null;
        }

        var convertedArgs = new object[cmd.maxParameters];
        for (int i = 0; i < providedCount; ++i) {
            var paramType = cmd.parameterInfos[i].ParameterType;
            try {
                convertedArgs[i] = ParserLocator.ParseTo(paramType, args[i]);
            }
            catch {
                SystemMessage($"Invalid parameter: command \"{name}\" expects the parameter \"{cmd.parameterInfos[i].Name}\" to be of type {paramType.Name}, but the provided parameter (\"{args[i]}\") could not be converted to it!");
                return null;
            }
        }

        // append default values to call, if any exist
        if (cmd.maxParameters > providedCount) {
            for (int i = providedCount; i < cmd.maxParameters; ++i) {
                convertedArgs[i] = cmd.parameterInfos[i].DefaultValue;
            }
        }

        try {
            var result = cmd.Invoke(convertedArgs);
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