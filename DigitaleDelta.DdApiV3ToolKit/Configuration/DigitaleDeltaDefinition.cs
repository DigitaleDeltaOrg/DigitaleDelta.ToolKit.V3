namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Represents the definition of a Digitale Delta data field, including its metadata and configuration settings.
/// </summary>
public class DigitaleDeltaDefinition
{
    /// <summary>
    /// Name
    /// </summary>
    public string? Name { set; get; }
    /// <summary>
    /// OData data type
    /// </summary>
    public string? ODataDataType { init; get; }
    /// <summary>
    /// Description
    /// </summary>
    public string? Description { init; get; }
    /// <summary>
    /// URL to definition of the data field
    /// </summary>
    public string? Definition { init; get; }
    /// <summary>
    /// Definition system
    /// </summary>
    public string? System { init; get; }
    /// <summary>
    /// Disallow in filter
    /// </summary>
    public bool? DisallowInFilter { init; get; }
}
