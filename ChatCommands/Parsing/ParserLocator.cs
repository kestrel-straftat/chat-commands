using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ChatCommands.Parsing;

public static class ParserLocator
{
    private static Dictionary<Type, ITypeParseExtension> m_extensions = [];

    static ParserLocator() {
        // register all chat commands' default parse extensions
        RegisterParseExtensionsFromAssembly();
    }

    /// <summary>
    /// Registers all <see cref="ITypeParseExtension"/> implementations from the calling assembly.
    /// </summary>
    public static void RegisterParseExtensionsFromAssembly() => RegisterParseExtensionsFromAssembly(Assembly.GetCallingAssembly());
    
    /// <summary>
    /// Registers all <see cref="ITypeParseExtension"/> implementations from a specified assembly.
    /// </summary>
    public static void RegisterParseExtensionsFromAssembly(Assembly assembly) {
        var extensionTypes = assembly
            .GetTypes()
            .Where(t => typeof(ITypeParseExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        foreach (var type in extensionTypes) {
            var instance = Activator.CreateInstance(type) as ITypeParseExtension;
            m_extensions.TryAdd(instance!.Target, instance);
        }
    }
    
    /// <summary>
    /// Converts the specified string representation of a value to its equivalent
    /// of the specified type. Tries registered <see cref="ITypeParseExtension"/> implementations, then tries
    /// <see cref="IConvertible"/>.
    /// </summary>
    /// <param name="type">The type to convert the value to.</param>
    /// <param name="input">A string containing the value to convert.</param>
    /// <returns>An object containing the converted value.</returns>
    /// <exception cref="NotSupportedException">The conversion is unsupported by <see cref="ParserLocator"/>.</exception>
    public static object ParseTo(Type type, string input) {
        // lazily handle simple nullable types (passing null to a command doesn't really *mean* anything anyway)
        if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            type = Nullable.GetUnderlyingType(type)!;
        }

        if (m_extensions.TryGetValue(type, out var parser)) {
            return parser.Parse(input);
        }
        if (typeof(IConvertible).IsAssignableFrom(type)) {
            return Convert.ChangeType(input, type);
        }
        
        throw new NotSupportedException("Type not supported.");
    }

    // these are basically just syntactic sugar. is this "good"? complain
    // at me on discord if it isn't. thanks
    
    public static bool TryParseTo(Type type, string input, out object result) {
        try {
            result = ParseTo(type, input);
        }
        catch {
            result = null;
            return false;
        }
        return true;
    }
    
    public static T ParseTo<T>(string input) => (T)ParseTo(typeof(T), input);
    
    public static bool TryParseTo<T>(string input, out T result) {
        try {
            result = ParseTo<T>(input);
        }
        catch {
            result = default;
            return false;
        }
        return true;
    }
}