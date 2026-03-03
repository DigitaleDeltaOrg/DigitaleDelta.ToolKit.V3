// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Defines a contract for implementing custom authentication handlers in an application.
/// </summary>
public interface IAuthenticationHandler
{
    /// <summary>
    /// Attempts to authenticate the incoming HTTP request based on the underlying authentication mechanism.
    /// </summary>
    /// <param name="context">The current HTTP context containing the request to be authenticated.</param>
    /// <param name="principal">
    /// When this method returns, contains the authenticated ClaimsPrincipal if authentication was successful;
    /// otherwise, null if authentication fails.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// true if authentication is successful; otherwise, false.
    /// </returns>
    Task<bool> TryAuthenticateAsync(HttpContext context, out ClaimsPrincipal? principal);
}