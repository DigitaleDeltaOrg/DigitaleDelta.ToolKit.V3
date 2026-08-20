namespace DigitaleDelta.CoverageJSON.Models;

public record CoverageDomain
{
    public required string Type { get; init; } // "Domain"
    public required string DomainType { get; init; }
    public required Dictionary<string, CoverageAxis> Axes { get; init; }
    public List<CoverageReferencing>? Referencing { get; init; }
}
