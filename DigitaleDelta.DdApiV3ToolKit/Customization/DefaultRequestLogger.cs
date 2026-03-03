// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using DigitaleDelta.QueryService;
using Serilog;

namespace DigitaleDelta.DdApiV3ToolKit.Customization;

/// <summary>
/// Default implementation of IRequestLogger that logs to console/file via Serilog.
/// </summary>
public class DefaultRequestLogger : IRequestLogger
{
    /// <summary>
    /// Logs request details using Serilog.
    /// </summary>
    public void LogRequest(string url, ClaimsPrincipal? claimsPrincipal, string requestId, DateTime requestStart)
    {
        var userId = claimsPrincipal?.Identity?.Name ?? "Anonymous";
        Log.Information("Request {RequestId}: {Method} {Url} by {User} at {RequestStart}",
            requestId, "GET", url, userId, requestStart);
    }

    /// <summary>
    /// Logs response details using Serilog.
    /// </summary>
    public void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null)
    {
        if (succeeded)
        {
            Log.Information("Response {RequestId}: Success in {Duration}ms, Size: {ResponseSize} bytes",
                requestId, duration.TotalMilliseconds, responseSize ?? 0);
        }
        else
        {
            Log.Warning("Response {RequestId}: Failed in {Duration}ms, Message: {Message}",
                requestId, duration.TotalMilliseconds, message ?? "Unknown error");
        }
    }
}
