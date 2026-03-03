using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitaleDelta.ODataWriter;

/// <summary>
/// A JSON converter designed for serializing a Dictionary&lt;string, object? &gt;.
/// Keys representing slash-separated paths are expanded into nested JSON objects during serialization.
/// </summary>
public sealed class SlashPathExpandingDictionaryConverter : JsonConverter<Dictionary<string, object?>>
{
    /// <summary>
    /// A thread-safe cache that stores precomputed slash-separated path segments.
    /// This cache is used to optimize the performance of splitting and reusing
    /// slash-separated paths during serialization in the
    /// <see cref="SlashPathExpandingDictionaryConverter"/>.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string[]> PathCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Reads a JSON representation of a Dictionary&lt;string, object?&gt;
    /// from the specified Utf8JsonReader. This method is not supported for the current converter.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader instance positioned at the JSON to read.</param>
    /// <param name="typeToConvert">The type of object to be converted.</param>
    /// <param name="options">The serialization options to use during reading.</param>
    /// <returns>Throws a NotSupportedException, as reading functionality is not implemented.</returns>
    /// <exception cref="NotSupportedException">Always thrown because reading is not supported by this converter.</exception>
    public override Dictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException("Reading not supported.");

    /// <summary>
    /// Writes a Dictionary&lt;string, object?&gt; to a JSON writer, expanding keys that represent slash-separated paths
    /// into nested JSON objects.
    /// </summary>
    /// <param name="writer">The writer to which the JSON output will be written.</param>
    /// <param name="value">The dictionary to serialize into JSON format.</param>
    /// <param name="options">Serialization options to apply, such as converters and format settings.</param>
    public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var root = new Node();
        
        foreach (var (key, val) in value)
        {
            var parts = PathCache.GetOrAdd(key, k => k.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            var node = root;

            for (var i = 0; i < parts.Length - 1; i++)
            {
                node = node.GetOrAddChild(parts[i]);
            }

            node.AddLeaf(parts[^1], val);
        }

        root.Write(writer, options);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Represents a hierarchical structure for managing nodes and their relationships as part of a JSON serialization process.
    /// </summary>
    private sealed class Node
    {
        private Dictionary<string, Node>? _children; // Lookup for fast access to child nodes by name.
        private List<Entry>? _order;                 // Keep order (child of leaf)

        /// <summary>
        /// Represents an immutable structure that holds information about a hierarchical node or a leaf node within a JSON serialization process.
        /// </summary>
        private readonly struct Entry(bool isChild, string name, Node? child, object? value)
        {
            public readonly bool    IsChild = isChild;
            public readonly string  Name    = name;
            public readonly Node?   Child   = child;
            public readonly object? Value   = value;
        }

        /// <summary>
        /// Retrieves an existing child node with the specified name or creates a new child node if it does not exist.
        /// </summary>
        /// <param name="name">The name of the child node to retrieve or create.</param>
        /// <returns>The existing or newly created child node associated with the specified name.</returns>
        public Node GetOrAddChild(string name)
        {
            _children ??= new Dictionary<string, Node>(4, StringComparer.Ordinal);
            
            if (_children.TryGetValue(name, out var child))
            {
                return child;
            }
            
            child = new Node();
            _children[name] = child;
            (_order ??= new List<Entry>(4)).Add(new Entry(isChild: true, name, child, null));

            return child;
        }

        /// <summary>
        /// Adds a leaf node with the given name and value to the current node.
        /// </summary>
        /// <param name="name">The name of the leaf node to be added.</param>
        /// <param name="value">The value associated with the leaf node.</param>
        public void AddLeaf(string name, object? value)
        {
            (_order ??= new List<Entry>(4)).Add(new Entry(isChild: false, name, null, value));
        }

        /// <summary>
        /// Writes a JSON representation of a Dictionary&lt;string, object?&gt; to the specified Utf8JsonWriter with expanded
        /// paths based on the keys split by slashes into a hierarchical JSON structure.
        /// </summary>
        /// <param name="writer">The Utf8JsonWriter instance to which the data is written.</param>
        /// <param name="options">The serialization options to be used during writing.</param>
        public void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            if (_order is null || _order.Count == 0)
            {
                return;
            }

            foreach (var e in _order)
            {
                if (e.IsChild)
                {
                    writer.WritePropertyName(e.Name);
                    writer.WriteStartObject();
                    e.Child?.Write(writer, options);
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WritePropertyName(e.Name);
                    WriteValue(writer, e.Value, options);
                }
            }
        }

        /// <summary>
        /// Writes a value to the specified Utf8JsonWriter based on its type.
        /// Handles writing of null values, dictionaries, collections, and primitive types.
        /// </summary>
        /// <param name="writer">The Utf8JsonWriter instance to which the value will be written.</param>
        /// <param name="v">The value to write. It can be null, a primitive type, a dictionary, or a collection.</param>
        /// <param name="options">The serialization options to use when writing the value.</param>
        private static void WriteValue(Utf8JsonWriter writer, object? v, JsonSerializerOptions options)
        {
            switch (v)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                
                case Dictionary<string, object?> dict:
                    writer.WriteStartObject();
                    foreach (var kv in dict) 
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteValue(writer, kv.Value, options);
                    }
                    writer.WriteEndObject();
                    break;
                
                case IEnumerable<object?> list when v is not string:
                    writer.WriteStartArray();
                    
                    foreach (var item in list)
                    {
                        WriteValue(writer, item, options);
                    }
                    
                    writer.WriteEndArray();
                    break;
                
                default:
                    JsonSerializer.Serialize(writer, v, v.GetType(), options);
                    break;
            }
        }
    }
}