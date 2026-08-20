using System.Text.Json;

namespace DigitaleDelta.CoverageJSON.Models;

public record CoverageAxis
{
    public required string Name { get; set; }
    public string? Type { get; set; }
    public double? Start { get; set; }
    public double? Stop { get; set; }
    public double? Spacing { get; set; }
    public int? Num { get; set; }
    public IReadOnlyList<JsonElement>? Values { get; init; }
}
