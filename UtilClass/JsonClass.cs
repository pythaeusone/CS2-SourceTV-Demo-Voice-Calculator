using System.Text.Json;

namespace CS2SourceTVDemoVoiceCalc.UtilClass
{
    /// <summary>
    /// Provides static helper methods for reading and writing key-value pairs to a JSON configuration file.
    /// The configuration is stored as a dictionary in "config.json".
    /// </summary>
    public static class JsonClass
    {
        /// <summary>
        /// The file path where the JSON configuration is stored.
        /// </summary>
        private static readonly string FilePath = "Config.json";

        /// <summary>
        /// Reads the entire JSON file and returns it as a dictionary.
        /// If the file does not exist or is empty, an empty dictionary is returned.
        /// </summary>
        private static Dictionary<string, object> ReadAll()
        {
            if (!File.Exists(FilePath))
                return new Dictionary<string, object>();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Writes the given dictionary data to the JSON file, overwriting any existing content.
        /// </summary>
        private static void WriteAll(Dictionary<string, object> data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(FilePath, json);
        }

        /// <summary>
        /// Writes a key-value pair to the JSON configuration file. If the key already exists, its value is updated.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The value to store.</param>
        public static void WriteJson(string key, object value)
        {
            var data = ReadAll();
            data[key] = value;
            WriteAll(data);
        }

        /// <summary>
        /// Checks if a given key exists in the JSON configuration file.
        /// </summary>
        /// <param name="key">The configuration key to check.</param>
        /// <returns>True if the key exists; otherwise, false.</returns>
        public static bool KeyExists(string key)
        {
            var data = ReadAll();
            return data.ContainsKey(key);
        }

        /// <summary>
        /// Reads a value from the JSON configuration file and deserializes it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The configuration key.</param>
        /// <returns>The value if found and successfully deserialized; otherwise, the default value of T.</returns>
        public static T? ReadJson<T>(string key)
        {
            var data = ReadAll();
            if (data.ContainsKey(key))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(data[key]));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }
    }
}
