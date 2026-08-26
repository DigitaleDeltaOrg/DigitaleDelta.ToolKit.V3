using DigitaleDelta.DdApiV3ToolKit.Configuration;
using Xunit;

namespace DigitaleDelta.ODataWriter.Tests;

public class YamlJsonConverterTests
{
    [Fact]
    public void ConvertYamlToJson_BooleansAreNotQuoted()
    {
        var yaml = """
                   key1: true
                   key2: false
                   nested:
                     active: true
                     disabled: false
                   """;

        var json = YamlJsonConverter.ConvertYamlToJson(yaml);

        Assert.Contains("\"key1\": true", json);
        Assert.Contains("\"key2\": false", json);
        Assert.Contains("\"active\": true", json);
        Assert.Contains("\"disabled\": false", json);
    }

    [Fact]
    public void ConvertYamlToJson_QuotedStringsRemainStrings()
    {
        var yaml = """
                   quotedTrue: "true"
                   quotedFalse: "false"
                   name: "sample"
                   """;

        var json = YamlJsonConverter.ConvertYamlToJson(yaml);

        Assert.Contains("\"quotedTrue\": \"true\"", json);
        Assert.Contains("\"quotedFalse\": \"false\"", json);
        Assert.Contains("\"name\": \"sample\"", json);
    }

    [Fact]
    public void ConvertYamlToJson_NumbersAndListsArePreserved()
    {
        var yaml = """
                   integerVal: 42
                   doubleVal: 3.14
                   items:
                     - 1
                     - true
                     - false
                     - "text"
                   """;

        var json = YamlJsonConverter.ConvertYamlToJson(yaml);

        Assert.Contains("\"integerVal\": 42", json);
        Assert.Contains("\"doubleVal\": 3.14", json);
        Assert.Contains("1,", json);
        Assert.Contains("true,", json);
        Assert.Contains("false,", json);
        Assert.Contains("\"text\"", json);
    }
}
