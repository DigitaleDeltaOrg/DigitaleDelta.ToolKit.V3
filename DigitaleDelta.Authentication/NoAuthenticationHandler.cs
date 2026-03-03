// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Provides authentication handling for scenarios where no authentication mechanism is required.
/// </summary>
/// <remarks>
/// This handler assigns an anonymous identity with a unique session ID to the user.
/// It is intended for use in environments where authentication is optional or not enforced.
/// </remarks>
public class NoAuthenticationHandler : IAuthenticationHandler
{
    /// <summary>
    /// Attempts to authenticate a user based on the provided HTTP context. If successful, sets the authenticated user principal.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <param name="principal">
    /// When successful, this is set to the authenticated user's claims principal; otherwise, it is set to null.
    /// </param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating the success of the authentication attempt.</returns>
    public Task<bool> TryAuthenticateAsync(HttpContext context, out ClaimsPrincipal? principal)
    {
        var sessionId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new("anonymous", "true"),
            new("anon.sessionid", sessionId)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "None");

        principal = new ClaimsPrincipal(identity);

        return Task.FromResult(true);
    }
}
