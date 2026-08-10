public record CoverageReferencing
{
    public required string Href { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public string? Title { get; set; }
}