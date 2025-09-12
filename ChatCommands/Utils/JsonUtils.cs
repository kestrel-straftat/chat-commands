using System.IO;
using Newtonsoft.Json;

namespace ChatCommands.Utils;

internal static class JsonUtils
{
    public static T FromJsonFile<T>(string path) {
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void ToJsonFile<T>(T obj, string path) {
        File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
    }
}
