// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Contents of application files, shared between components.
/// </summary>
public class AppFileContents
{
    /// <summary>
    /// Textual representation of the CSDL model from the template.
    /// </summary>
    public string CsdlText { get; init; } = string.Empty;
    /// <summary>
    /// Textual representation of the OpenAPI specification from the template.
    /// </summary>
    public string OpenApiText { get; init; } = string.Empty;
    /// <summary>
    /// Textual representation of the SVG diagram from the template.
    /// </summary>
    public string SvgText { get; init; } = string.Empty;
    /// <summary>
    /// Official context properties
    /// </summary>
    public Dictionary<string, string> ContextProperties { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
    /// <summary>
    /// Official reference properties
    /// </summary>
    public Dictionary<string, string> ReferenceProperties { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
    /// <summary>
    /// Official metadata properties
    /// </summary>
    public Dictionary<string, string> MetadataProperties { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
}
