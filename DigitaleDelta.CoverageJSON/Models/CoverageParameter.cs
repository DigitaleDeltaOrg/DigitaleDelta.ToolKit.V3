namespace DigitaleDelta.CoverageJSON.Models;

public record CoverageParameter
{
    public required string Name { get; set; }
    public string? ObservedProperty { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public string? Range { get; set; }
}