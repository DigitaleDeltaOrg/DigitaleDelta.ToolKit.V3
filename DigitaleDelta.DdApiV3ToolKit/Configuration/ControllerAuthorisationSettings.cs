// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Per-controller authorisation configuration. Keyed by controller name (e.g. "ObservationController").
/// </summary>
public class ControllerAuthorisationEntry
{
    /// <summary>
    /// Authentication type expected for this controller. "None" disables the authorisation plugin
    /// and uses a default-allow handler. Any other value requires <see cref="AuthorisationHandler"/> to be set.
    /// </summary>
    public string AuthenticationType { get; set; } = "None";

    /// <summary>
    /// Fully qualified type name of the IAuthorisation implementation to use for this controller.
    /// Format: Namespace.ClassName, AssemblyName. Ignored when <see cref="AuthenticationType"/> is "None".
    /// </summary>
    public string? AuthorisationHandler { get; set; }
}
