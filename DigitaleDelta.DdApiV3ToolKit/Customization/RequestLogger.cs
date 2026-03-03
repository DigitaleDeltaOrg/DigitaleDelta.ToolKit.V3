// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using DigitaleDelta.QueryService;
using Serilog;

namespace DigitaleDelta.DdApiV3ToolKit.Customization;

#pragma warning disable CS9113 // Parameter is unread.

/// <summary>
/// Implement. Log requests and response definition.
/// </summary>
public class RequestLogger(IConfiguration configuration) : IRequestLogger
{
    /// <summary>
    /// TODO: Implement logging logic. configuration can be used to read any settings needed.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="claimsPrincipal"></param>
    /// <param name="requestId"></param>
    /// <param name="requestStart"></param>
    public void LogRequest(string url, ClaimsPrincipal? claimsPrincipal, string requestId, DateTime requestStart)
    {
        // Implement your own logic.
        Log.Information("Request: {requestId} - {url} - {requestStart}", requestId, url, requestStart);
    }

    /// <summary>
    /// TODO: Implement logging logic. configuration can be used to read any settings needed.
    /// </summary>
    /// <param name="requestId"></param>
    /// <param name="succeeded"></param>
    /// <param name="requestEnd"></param>
    /// <param name="duration"></param>
    /// <param name="responseSize"></param>
    /// <param name="message"></param>
    public void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null)
    {
        // Implement your own logic.
        Log.Information("Response: {requestId} - {succeeded} - {duration} - {responseSize} - {message}", requestId, succeeded, duration, responseSize, message);
    }
}
