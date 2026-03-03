// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Base parameters to pass on to controllers.
/// </summary>
public class BaseParameters
{
    /// <summary>
    /// Prefix character for parameters in SQL queries.
    /// </summary>
    public char ParameterPrefix { get; init; } = '@';
}