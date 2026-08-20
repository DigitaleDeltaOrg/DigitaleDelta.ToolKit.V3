namespace DigitaleDelta.CoverageJSON.Models;

public record CoverageRange
{
    public required string Name { get; set; }
    public string? DataType { get; set; }
    public List<string>? AxisNames { get; set; }
    public List<int>? Shape { get; set; }
    public List<double>? Values { get; set; }
}