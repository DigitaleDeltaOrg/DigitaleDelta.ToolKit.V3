// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Disable init only warning. Fails in tests.
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace DigitaleDelta.SkipToken;

/// <summary>
/// Defines a skip token. 
/// </summary>
public class SkipToken
{
    /// <summary>
    /// Request URL
    /// </summary>
    public required string RequestUrl { set; get; }
    /// <summary>
    /// Last id
    /// </summary>
    public required string LastId { set; get; }
    /// <summary>
    /// Creation Date
    /// </summary>
    public required DateTimeOffset CreationDate { set; get; }
}