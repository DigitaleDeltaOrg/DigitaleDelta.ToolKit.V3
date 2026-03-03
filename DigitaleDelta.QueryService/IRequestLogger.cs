// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace DigitaleDelta.QueryService;

/// <summary>
/// Interface for logging requests and responses.
/// </summary>
public interface IRequestLogger
{
    /// <summary>
    /// Logs the details of an incoming request.
    /// </summary>
    /// <param name="url">Request URL with parameters, etc.</param>
    /// <param name="claimsPrincipal">Identified called.</param>
    /// <param name="requestId">Request Id, for request/response matching.</param>
    /// <param name="requestStart">Start time.</param>
    void LogRequest(string url, ClaimsPrincipal? claimsPrincipal, string requestId, DateTime requestStart);
    
    /// <summary>
    /// Logs the details of a response.
    /// </summary>
    /// <param name="requestId">Request Id, for request/response matching</param>
    /// <param name="succeeded">Indicates whether the response was successful.</param>
    /// <param name="requestEnd">The time when the request ended.</param>
    /// <param name="duration">Duration of the request.</param>
    /// <param name="responseSize">Size of the response in bytes.</param>
    /// <param name="message">Optional message related to the response.</param>
    void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null);
}
