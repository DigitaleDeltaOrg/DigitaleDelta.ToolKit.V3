using DigitaleDelta.DdApiV3ToolKit.Configuration;
using Microsoft.Extensions.Configuration;

namespace DigitaleDelta.ODataWriter.Tests;

public class ConfigurationValidatorTests
{
    [Fact]
    public void ValidateRequireFilter_WhenPropertyHasRequireInFilter_AndRequireFilterIsFalse_FailsValidation()
    {
        // Arrange
        ConfigurationValidator.IsValid = true;
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ObservationConfiguration:RequireFilter"] = "false",
            ["ObservationConfiguration:PropertyMapping:0:ODataPropertyName"] = "Id",
            ["ObservationConfiguration:PropertyMapping:0:Query"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:EdmType"] = "Edm.Int32",
            ["ObservationConfiguration:PropertyMapping:0:ColumnName"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:RequireInFilter"] = "true",
            ["ObservationConfiguration:PropertyMapping:1:ODataPropertyName"] = "Name",
            ["ObservationConfiguration:PropertyMapping:1:Query"] = "name",
            ["ObservationConfiguration:PropertyMapping:1:EdmType"] = "Edm.String",
            ["ObservationConfiguration:PropertyMapping:1:ColumnName"] = "name",
            ["ObservationConfiguration:PropertyMapping:1:RequireInFilter"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        ConfigurationValidator.ValidateRequireFilter(configuration, "ObservationConfiguration");

        // Assert
        Assert.False(ConfigurationValidator.IsValid);
    }

    [Fact]
    public void ValidateRequireFilter_WhenPropertyHasRequireInFilter_AndRequireFilterIsTrue_PassesValidation()
    {
        // Arrange
        ConfigurationValidator.IsValid = true;
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ObservationConfiguration:RequireFilter"] = "true",
            ["ObservationConfiguration:PropertyMapping:0:ODataPropertyName"] = "Id",
            ["ObservationConfiguration:PropertyMapping:0:Query"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:EdmType"] = "Edm.Int32",
            ["ObservationConfiguration:PropertyMapping:0:ColumnName"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:RequireInFilter"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        ConfigurationValidator.ValidateRequireFilter(configuration, "ObservationConfiguration");

        // Assert
        Assert.True(ConfigurationValidator.IsValid);
    }

    [Fact]
    public void ValidateRequireFilter_WhenNoPropertyHasRequireInFilter_AndRequireFilterIsFalse_PassesValidation()
    {
        // Arrange
        ConfigurationValidator.IsValid = true;
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ObservationConfiguration:RequireFilter"] = "false",
            ["ObservationConfiguration:PropertyMapping:0:ODataPropertyName"] = "Id",
            ["ObservationConfiguration:PropertyMapping:0:Query"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:EdmType"] = "Edm.Int32",
            ["ObservationConfiguration:PropertyMapping:0:ColumnName"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:RequireInFilter"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        ConfigurationValidator.ValidateRequireFilter(configuration, "ObservationConfiguration");

        // Assert
        Assert.True(ConfigurationValidator.IsValid);
    }

    [Fact]
    public void ValidateRequireFilter_WhenNoPropertyHasRequireInFilter_AndRequireFilterIsTrue_PassesValidation()
    {
        // Arrange
        ConfigurationValidator.IsValid = true;
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ObservationConfiguration:RequireFilter"] = "true",
            ["ObservationConfiguration:PropertyMapping:0:ODataPropertyName"] = "Id",
            ["ObservationConfiguration:PropertyMapping:0:Query"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:EdmType"] = "Edm.Int32",
            ["ObservationConfiguration:PropertyMapping:0:ColumnName"] = "id",
            ["ObservationConfiguration:PropertyMapping:0:RequireInFilter"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        ConfigurationValidator.ValidateRequireFilter(configuration, "ObservationConfiguration");

        // Assert
        Assert.True(ConfigurationValidator.IsValid);
    }
}
