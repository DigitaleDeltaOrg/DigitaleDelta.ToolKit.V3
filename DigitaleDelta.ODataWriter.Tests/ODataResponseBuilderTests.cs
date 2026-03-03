namespace DigitaleDelta.ODataWriter.Tests;

public class ODataResponseBuilderTests
{
    [Fact]
    public void Build_Sorts_Id_First_Then_Others_Alphabetically()
    {
        var entities = new[]
        {
            new Dictionary<string, object?> { ["Name"] = "Test", ["Id"] = 1, ["Quantity"] = 5 }
        };

        var builder = new ODataResponseBuilder()
            .WithEntities(entities)
            .WithSelectProperties(new[] { "Id", "Name", "Quantity" });

        var response = builder.Build(applySort: true);

        var keys = response.Value[0].Keys;
        Assert.Equal(new[] { "Id", "Name", "Quantity" }, keys);
    }

    [Fact]
    public void Build_Sorts_Without_Id_Alphabetically()
    {
        var entities = new[]
        {
            new Dictionary<string, object?> { ["Name"] = "Test", ["Quantity"] = 5 }
        };

        var builder = new ODataResponseBuilder()
            .WithEntities(entities)
            .WithSelectProperties(new[] { "Name", "Quantity" });

        var response = builder.Build(applySort: true);

        var keys = response.Value[0].Keys;
        Assert.Equal(new[] { "Name", "Quantity" }, keys);
    }

    [Fact]
    public void Build_Applies_SelectProperties()
    {
        var entities = new[]
        {
            new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "Test", ["Quantity"] = 5 }
        };

        var builder = new ODataResponseBuilder()
            .WithEntities(entities)
            .WithSelectProperties(["Name"]);

        var response = builder.Build(applySort: true);
        var dict = response.Value[0];
        
        Assert.Single(dict);
        Assert.True(dict.ContainsKey("Name"));
    }

    [Fact]
    public void Build_Includes_Count_If_Requested()
    {
        var entities = new[]
        {
            new Dictionary<string, object?> { ["Id"] = 1 }
        };

        var builder = new ODataResponseBuilder()
            .WithEntities(entities)
            .IncludeCount()
            .WithPagination((string?)null, 42);

        var response = builder.Build();

        Assert.Equal(42, response.Count);
    }

    [Fact]
    public void Build_Sets_NextLink_If_MoreData()
    {
        var entities = new[]
        {
            new Dictionary<string, object?> { ["Id"] = 1 }
        };

        var builder = new ODataResponseBuilder()
            .WithEntities(entities)
            .WithBaseUrl("https://example.com/odata")
            .WithEntitySet("Products")
            .WithQuery("$top=10")
            .HasMoreData(true)
            .WithPagination(new Guid("11111111-1111-1111-1111-111111111111"));

        var response = builder.Build();

        Assert.Contains("$skiptoken=11111111-1111-1111-1111-111111111111", response.NextLink);
    }
}