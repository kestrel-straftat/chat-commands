using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChatCommands.Utils;

internal static class CollectionExtensions
{
    /// <returns>An infinite enumerable of random values from the provided dictionary.</returns>
    public static IEnumerable<TValue> RandomValues<TKey, TValue>(this IDictionary<TKey, TValue> dict) {
        var values = dict.Values.ToArray();
        int size = dict.Count;
        while (true) {
            yield return values[Random.Range(0, size - 1)];
        }
    }
}