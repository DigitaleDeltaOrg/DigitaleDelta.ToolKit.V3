// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

/// <summary>
/// Represents the configuration settings for a rate-limiting policy.
/// This configuration controls how requests are limited for a specific
/// policy based on defined thresholds and time windows.
/// </summary>
public class RateLimitingPolicyConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the rate-limiting policy is enabled.
    /// When set to true, the policy will be applied to incoming requests based on
    /// its defined configuration. If false, the policy will be ignored.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the name of the rate-limiting policy.
    /// This value serves as a unique identifier for the policy, allowing it
    /// to be referenced and applied when configuring rate-limiting behavior.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the maximum number of requests permitted during a configured time window.
    /// This value dictates the upper limit of allowed requests before rate limiting is enforced
    /// for a specific policy.
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// Gets or sets the duration of the rate-limiting time window in seconds.
    /// This value defines the time span during which the specified number of requests
    /// (configured via <see cref="PermitLimit"/>) is allowed for a partition key.
    /// </summary>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// Gets or sets the partition key used to classify and group requests
    /// for rate-limiting purposes. This key determines how incoming requests
    /// are partitioned based on attributes such as user ID, API key, IP address,
    /// or other identifiers.
    /// </summary>
    public string PartitionKey { get; set; } = ""; // e.g. "api_key", "sub", "ip"
}