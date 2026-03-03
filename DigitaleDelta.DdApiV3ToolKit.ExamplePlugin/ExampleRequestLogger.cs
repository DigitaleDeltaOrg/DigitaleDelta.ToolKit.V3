// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using DigitaleDelta.QueryService;
using Microsoft.Extensions.Configuration;

namespace DigitaleDelta.DdApiV3ToolKit.ExamplePlugin;

/// <summary>
/// Example implementation of IRequestLogger that logs to a custom destination.
/// In production, implement your own logging logic (database, file, external service, etc.).
/// </summary>
public class ExampleRequestLogger : IRequestLogger
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor with dependency injection support.
    /// You can inject any service registered in the application's DI container.
    /// </summary>
    public ExampleRequestLogger(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Log request details - customise based on your requirements.
    /// </summary>
    public void LogRequest(string url, ClaimsPrincipal? claimsPrincipal, string requestId, DateTime requestStart)
    {
        var userId = claimsPrincipal?.Identity?.Name ?? "Anonymous";
        var userEmail = claimsPrincipal?.FindFirst(ClaimTypes.Email)?.Value ?? "N/A";

        // Example: Log to console (replace with your logging mechanism)
        Console.WriteLine($"[{requestStart:yyyy-MM-dd HH:mm:ss}] REQUEST {requestId}");
        Console.WriteLine($"  User: {userId} ({userEmail})");
        Console.WriteLine($"  URL: {url}");

        // TODO: Implement your custom logging
        // Examples:
        // - Write to database
        // - Send to external logging service
        // - Write to custom log file
        // - Send metrics to monitoring system
    }

    /// <summary>
    /// Log response details - customise based on your requirements.
    /// </summary>
    public void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null)
    {
        // Example: Log to console (replace with your logging mechanism)
        Console.WriteLine($"[{requestEnd:yyyy-MM-dd HH:mm:ss}] RESPONSE {requestId}");
        Console.WriteLine($"  Status: {(succeeded ? "SUCCESS" : "FAILED")}");
        Console.WriteLine($"  Duration: {duration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Size: {responseSize ?? 0} bytes");

        if (!string.IsNullOrEmpty(message))
        {
            Console.WriteLine($"  Message: {message}");
        }

        // TODO: Implement your custom logging
        // Examples:
        // - Write to database
        // - Send to external logging service
        // - Calculate and store performance metrics
        // - Alert on slow requests
    }
}
