// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Rate limiting configuration. 
/// </summary>
public class RateLimitingConfig
{
    /// <summary>
    /// Policies for rate limiting.
    /// </summary>
    public List<RateLimitingPolicyConfig> Policies { get; init; } = new();
}
