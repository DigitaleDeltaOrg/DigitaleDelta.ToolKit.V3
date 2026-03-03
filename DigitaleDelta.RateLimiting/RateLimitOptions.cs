// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace DigitaleDelta.RateLimiting;

/// <summary>
/// Represents configuration options for rate limiting settings.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Represents the Rate Limit applied to a specific action in the API.
    /// </summary>
    public int Limit { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public int NumberOfConcurrentRequests { get; set; }

    /// <summary>
    /// Represents the unit used for rate limiting.
    /// </summary>
    public string Unit { get; set; } = string.Empty;
}