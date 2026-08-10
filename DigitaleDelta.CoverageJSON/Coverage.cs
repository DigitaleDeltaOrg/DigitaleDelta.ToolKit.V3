public record Coverage
{
    public required string Type { get; init; } // "Coverage"
    public string? Id { get; init; }
    public required CoverageDomain Domain { get; init; }
    public required Dictionary<string, CoverageParameter> Parameters { get; init; }
    public required Dictionary<string, CoverageRange> Ranges { get; init; }
}