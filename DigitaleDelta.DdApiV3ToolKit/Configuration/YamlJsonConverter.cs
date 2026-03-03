// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

public static class YamlJsonConverter
{
    /// <summary>
    /// JSON serializer options.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Converts a YAML string to a JSON string.
    /// </summary>
    /// <param name="yaml">Yaml string</param>
    /// <param name="indented">Indentation</param>
    /// <returns>Json, converted from YAML</returns>
    public static string ConvertYamlToJson(string yaml, bool indented = true)
    {
        var deserializer = new DeserializerBuilder().Build();
        var obj = deserializer.Deserialize<object>(yaml);
        var normalized = Normalize(obj);

        return JsonSerializer.Serialize(normalized, _jsonOptions);
    }

    /// <summary>
    /// Normalises an object by converting dictionaries and enumerable items recursively.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static object? Normalize(object? node)
    {
        switch (node)
        {
            case null:
            {
                return null;
            }

            case IDictionary dict:
            {
                var result = new Dictionary<string, object?>();

                foreach (DictionaryEntry entry in dict)
                {
                    var key = entry.Key.ToString() ?? string.Empty;

                    result[key] = Normalize(entry.Value);
                }

                return result;
            }

            case IEnumerable enumerable when !(node is string):
            {
                return (from object? item in enumerable select Normalize(item)).ToList();
            }

            default:
            {
                return node;
            }
        }
    }
}
