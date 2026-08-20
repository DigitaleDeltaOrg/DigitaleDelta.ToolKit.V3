namespace DigitaleDelta.QueryService;

public class CoverageConfiguration
{
    public string? CoverageProperty { get; set; }
    public DateTimeOffset? SliceBegin { get; set; }
    public DateTimeOffset? SliceEnd { get; set; }
    public string? AggregateFunction { get; set; }
    public string? AggregateDuration { get; set; }
    public List<string>? SelectAxes { get; set; }
    public List<string>? SelectRanges { get; set; }
    public ICoverageTransformer? Transformer { get; set; }

    public List<string> Errors { get; set; } = new();
}
