using DigitaleDelta.Contracts.Configuration;
using DigitaleDelta.ErrorHandling;
using DigitaleDelta.ODataTranslator;
using DigitaleDelta.RequestHelpers;
using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.ODataWriter.Tests;

public class RequireInFilterTests
{
    private static Dictionary<string, ODataToSqlMap> CreatePropertyMaps(bool idRequiredInFilter)
    {
        return new Dictionary<string, ODataToSqlMap>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = new()
            {
                ODataPropertyName = "Id",
                Query = "id",
                EdmType = "Edm.Int32",
                ColumnName = "id",
                RequireInFilter = idRequiredInFilter
            },
            ["Name"] = new()
            {
                ODataPropertyName = "Name",
                Query = "name",
                EdmType = "Edm.String",
                ColumnName = "name",
                RequireInFilter = false
            }
        };
    }

    private static Dictionary<string, ODataFunctionMap> CreateFunctionMaps()
    {
        return new Dictionary<string, ODataFunctionMap>(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToODataQueryOptions_WhenRequireFilterIsFalse_AndRequireInFilterIsTrue_MissingFilterThrowsException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";

        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: true);
        var functionMaps = CreateFunctionMaps();

        // Act & Assert
        var ex = Assert.Throws<ODataValidationException>(() =>
            request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: false));

        Assert.Equal("MissingFilter", ex.Code);
        Assert.Equal("Service requires $filter with required property (or properties): 'Id'", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToODataQueryOptions_WhenRequireFilterIsFalse_AndRequireInFilterIsTrue_EmptyFilterThrowsException(string emptyFilter)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";
        request.QueryString = new QueryString($"?$filter={emptyFilter}");

        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: true);
        var functionMaps = CreateFunctionMaps();

        // Act & Assert
        var ex = Assert.Throws<ODataValidationException>(() =>
            request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: false));

        Assert.Equal("MissingFilter", ex.Code);
        Assert.Equal("Service requires $filter with required property (or properties): 'Id'", ex.Message);
    }

    [Fact]
    public void ToODataQueryOptions_WhenRequireFilterIsTrue_AndNoRequireInFilter_MissingFilterThrowsGenericException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";

        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: false);
        var functionMaps = CreateFunctionMaps();

        // Act & Assert
        var ex = Assert.Throws<ODataValidationException>(() =>
            request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: true));

        Assert.Equal("MissingFilter", ex.Code);
        Assert.Equal("Service requires $filter", ex.Message);
    }

    [Fact]
    public void ToODataQueryOptions_WhenMultiplePropertiesRequireInFilter_MissingFilterListsAllProperties()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";

        var propertyMaps = new Dictionary<string, ODataToSqlMap>(StringComparer.OrdinalIgnoreCase)
        {
            ["PhenomenonTime/BeginPosition"] = new()
            {
                ODataPropertyName = "PhenomenonTime/BeginPosition",
                Query = "phenomenontime_beginposition",
                EdmType = "Edm.DateTimeOffset",
                ColumnName = "phenomenontime_beginposition",
                RequireInFilter = true
            },
            ["Foi/Code"] = new()
            {
                ODataPropertyName = "Foi/Code",
                Query = "foi_code",
                EdmType = "Edm.String",
                ColumnName = "foi_code",
                RequireInFilter = true
            }
        };
        var functionMaps = CreateFunctionMaps();

        // Act & Assert
        var ex = Assert.Throws<ODataValidationException>(() =>
            request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: false));

        Assert.Equal("MissingFilter", ex.Code);
        Assert.Equal("Service requires $filter with required property (or properties): 'PhenomenonTime/BeginPosition', 'Foi/Code'", ex.Message);
    }

    [Fact]
    public void ToODataQueryOptions_WhenRequireFilterIsFalse_AndRequireInFilterIsTrue_FilterMissingRequiredPropertyThrowsException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";
        request.QueryString = new QueryString("?$filter=Name eq 'test'");

        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: true);
        var functionMaps = CreateFunctionMaps();

        // Act & Assert
        var ex = Assert.Throws<ODataValidationException>(() =>
            request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: false));

        Assert.Equal("InvalidFilter", ex.Code);
        Assert.Contains("Property 'Id' is required but not found in filter expression", ex.Message);
    }

    [Fact]
    public void ToODataQueryOptions_WhenRequireFilterIsFalse_AndRequireInFilterIsTrue_FilterWithRequiredPropertySucceeds()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";
        request.QueryString = new QueryString("?$filter=Id eq 10");

        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: true);
        var functionMaps = CreateFunctionMaps();

        // Act
        var options = request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: false);

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.Filter);
    }

    [Fact]
    public void ToODataQueryOptions_WhenRequireFilterIsFalse_AndNoRequireInFilter_MissingFilterSucceeds()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.Path = "/api/v3/Observation";

        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: false);
        var functionMaps = CreateFunctionMaps();

        // Act
        var options = request.ToODataQueryOptions(propertyMaps, functionMaps, defaultTop: 100, maxTop: 1000, requireFilter: false);

        // Assert
        Assert.NotNull(options);
        Assert.Null(options.Filter);
    }

    [Fact]
    public void ODataFilterValidator_ValidatesRequireInFilter()
    {
        // Arrange
        var propertyMaps = CreatePropertyMaps(idRequiredInFilter: true);
        var functionMaps = CreateFunctionMaps();
        var validator = new ODataFilterValidator(propertyMaps, functionMaps);

        // Act - missing required Id property
        var parsedWithoutId = ODataFilter.TryParse("$filter=Name eq 'test'", out var filterWithoutId, out _);
        Assert.True(parsedWithoutId);
        var isValidWithoutId = validator.TryValidate(filterWithoutId?.Context, out var errorWithoutId);

        // Act - including required Id property
        var parsedWithId = ODataFilter.TryParse("$filter=Id eq 10 and Name eq 'test'", out var filterWithId, out _);
        Assert.True(parsedWithId);
        var isValidWithId = validator.TryValidate(filterWithId?.Context, out var errorWithId);

        // Assert
        Assert.False(isValidWithoutId);
        Assert.Contains("Property 'Id' is required but not found in filter expression", errorWithoutId);

        Assert.True(isValidWithId);
        Assert.Null(errorWithId);
    }
}
