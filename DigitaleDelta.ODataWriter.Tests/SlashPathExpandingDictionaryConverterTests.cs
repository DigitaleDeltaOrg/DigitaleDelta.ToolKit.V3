using System.Text.Json;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Converters;

namespace DigitaleDelta.ODataWriter.Tests;

public sealed class SlashPathExpandingDictionaryConverterTests
{
    private static JsonSerializerOptions Options() => new()
    {
        Converters =
        {
            new GeoJsonConverterFactory(),
            new SlashPathExpandingDictionaryConverter()
        }
    };

    [Fact]
    public void Serialize_MultiPolygon_WritesSingleGeoJsonMultiPolygon()
    {
        var geometry = new WKTReader().Read("MULTIPOLYGON (((0 0, 0 1, 1 1, 0 0)), ((2 2, 2 3, 3 3, 2 2)))");
        var value = new Dictionary<string, object?> { ["Geography"] = geometry };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, Options()));
        var geography = document.RootElement.GetProperty("Geography");

        Assert.Equal("MultiPolygon", geography.GetProperty("type").GetString());
        Assert.Equal(2, geography.GetProperty("coordinates").GetArrayLength());
    }

    [Fact]
    public void Serialize_Polygon_WritesGeoJsonPolygon()
    {
        var geometry = new WKTReader().Read("POLYGON ((0 0, 0 1, 1 1, 0 0))");
        var value = new Dictionary<string, object?> { ["Geography"] = geometry };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, Options()));

        Assert.Equal("Polygon", document.RootElement.GetProperty("Geography").GetProperty("type").GetString());
    }

    [Fact]
    public void Serialize_OrdinaryList_RemainsJsonArray()
    {
        var value = new Dictionary<string, object?> { ["Values"] = new object?[] { 1, "two", true } };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, Options()));

        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("Values").ValueKind);
    }

    [Fact]
    public void Serialize_SlashPath_ExpandsNestedObject()
    {
        var value = new Dictionary<string, object?> { ["Properties/Name"] = "value" };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, Options()));

        Assert.Equal("value", document.RootElement.GetProperty("Properties").GetProperty("Name").GetString());
    }
}
