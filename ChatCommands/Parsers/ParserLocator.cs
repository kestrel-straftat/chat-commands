using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChatCommands.Parsers;

public static class ParserLocator
{
    private static Dictionary<Type, ParserBase> m_parsers = [];

    static ParserLocator() {
        var parserTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ParserBase)) && !t.IsAbstract);
        
        foreach (var type in parserTypes) {
            var instance = Activator.CreateInstance(type) as ParserBase;
            m_parsers.TryAdd(instance!.ParseResultType, instance);
        }
    }

    /// <summary>
    /// Registers all <see cref="ParserBase"/> implementations from the calling assembly.
    /// </summary>
    public static void RegisterParsersFromAssembly() => RegisterParsersFromAssembly(Assembly.GetCallingAssembly());
    
    /// <summary>
    /// Registers all <see cref="ParserBase"/> implementations from a specified assembly.
    /// </summary>
    public static void RegisterParsersFromAssembly(Assembly assembly) {
        var parserTypes = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ParserBase)) && !t.IsAbstract);
        
        foreach (var type in parserTypes) {
            var instance = Activator.CreateInstance(type) as ParserBase;
            m_parsers.TryAdd(instance!.ParseResultType, instance);
        }
    }

    private static bool TryIConvertible(Type resultType, string input, out object result) {
        result = null;
        // If we explicitly define a converter for the type use that instead
        if (!typeof(IConvertible).IsAssignableFrom(resultType) || m_parsers.ContainsKey(resultType)) return false;
        result = Convert.ChangeType(input, resultType);
        return true;
    }

    public static object ParseTo(Type type, string input) {
        if (TryIConvertible(type, input, out var result)) {
            return result;
        }
        if (m_parsers.TryGetValue(type, out var parser)) {
            return parser.Parse(input);
        }
        throw new NotSupportedException("Type not supported.");
    }

    public static bool TryParseTo(Type type, string input, out object result) {
        if (TryIConvertible(type, input, out result)) return true;
        if (!m_parsers.TryGetValue(type, out var parser)) return false;
        try {
            result = parser.Parse(input);
        }
        catch { return false; }

        return true;
    }
    
    public static T ParseTo<T>(string input) => (T)ParseTo(typeof(T), input);
    
    public static bool TryParseTo<T>(string input, out T result) {
        result = default;
        if (!TryParseTo(typeof(T), input, out var resultObject)) return false;
        result = (T)resultObject;
        return true;
    }
}