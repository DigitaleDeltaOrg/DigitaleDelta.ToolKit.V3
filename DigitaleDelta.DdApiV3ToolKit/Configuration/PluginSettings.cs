// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Configuration settings for the plugin system.
/// </summary>
public class PluginSettings
{
    /// <summary>
    /// Directory where plugin DLLs are located. Defaults to "Plugins" in the application directory.
    /// </summary>
    public string PluginDirectory { get; set; } = "Plugins";

    /// <summary>
    /// Fully qualified type name of the IAuthorization implementation to use.
    /// Format: Namespace.ClassName, AssemblyName
    /// If not specified, a plugin must be found in the plugin directory.
    /// </summary>
    public string? AuthorizationHandler { get; set; }

    /// <summary>
    /// Fully qualified type name of the IRequestLogger implementation to use.
    /// Format: Namespace.ClassName, AssemblyName
    /// If not specified, the default console logger will be used.
    /// </summary>
    public string? RequestLogger { get; set; }
}
