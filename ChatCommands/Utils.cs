using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ChatCommands;

internal static class ExceptionExtensions
{
    public static Exception GetInnerMostException(this Exception ex) {
        while (ex.InnerException is not null && ex is not null)
            ex = ex.InnerException;

        return ex;
    }
}

internal static class Utils
{
    public static IEnumerable<TValue> RandomValues<TKey, TValue>(IDictionary<TKey, TValue> dict) {
        var values = dict.Values.ToList();
        int size = dict.Count;
        while (true) {
            yield return values[Random.Range(0, size - 1)];
        }
    }

    public static bool AnyInputFieldFocused() {
        var currentSelected = EventSystem.current.currentSelectedGameObject;
        if (!currentSelected) return false;
        var tmpField = currentSelected.GetComponent<TMP_InputField>();
        if (tmpField && tmpField.isFocused)
            return true;

        var unityField = currentSelected.GetComponent<InputField>();
        return unityField && unityField.isFocused;
    }
    
    public static T LoadFromJsonFile<T>(string path) {
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void SaveToJsonFile<T>(T obj, string path) {
        File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
    }
}
