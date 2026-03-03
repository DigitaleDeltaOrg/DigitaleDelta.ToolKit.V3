// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading.RateLimiting;

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Provides extension methods for configuring rate limiting within the application.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Configures rate limiting for the application by adding rate limiter policies
    /// based on the provided configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the rate limiter policies will be added.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance containing the rate limiting settings.</param>
    /// <returns>The <see cref="IServiceCollection"/> with the rate limiting configuration applied.</returns>
    public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSection = configuration.GetSection("RateLimiting");
        var rateLimitConfig = rateLimitSection.Get<RateLimitingConfig>();

        services.AddRateLimiter(options =>
        {
            if (rateLimitConfig?.Policies == null)
            {
                return;
            }
            
            foreach (var policy in rateLimitConfig.Policies.Where(p => p.Enabled))
            {
                // capture partition key type etc per policy
                options.AddPolicy(policy.Name, context =>
                {
                    var partitionKey = policy.PartitionKey switch
                    {
                        "sub"     => context.User.FindFirst("sub")?.Value ?? "anon",
                        "api_key" => context.User.FindFirst("api_key")?.Value ?? context.Request.Headers["X-API-KEY"].FirstOrDefault() ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                        "ip"      => context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                        "subject" => context.User.FindFirst("subject")?.Value ?? "anon",
                        _         => "anon"
                    };
                
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = policy.PermitLimit,
                            Window = TimeSpan.FromSeconds(policy.WindowSeconds)
                        });
                });
            }
        });
        
        return services;
    }
}