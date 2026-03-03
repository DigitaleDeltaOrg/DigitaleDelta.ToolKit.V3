// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Responsible for handling authentication using an API key retrieved from a specific HTTP request header.
/// Implements the <see cref="IAuthenticationHandler"/> interface.
/// </summary>
public class ApiKeyAuthenticationHandler(string headerName) : IAuthenticationHandler
{
    /// <summary>
    /// Attempts to authenticate a request using the specified API key from the HTTP request headers.
    /// </summary>
    /// <param name="context">The HTTP context containing the request to authenticate.</param>
    /// <param name="principal">
    /// When the method returns, contains the authenticated <see cref="ClaimsPrincipal"/>
    /// if authentication succeeds; otherwise, null.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value
    /// indicating whether authentication was successful.
    /// </returns>
    public Task<bool> TryAuthenticateAsync(HttpContext context, out ClaimsPrincipal? principal)
    {
        principal = null;
        
        if (context.Request.Headers.TryGetValue(headerName, out var apiKey))
        {
            // Hier kun je evt. claims bepalen o.b.v. apiKey
            var claims = new[] { new Claim("api_key", apiKey!) };
            var identity = new ClaimsIdentity(claims, authenticationType: "APIKEY");

            principal = new ClaimsPrincipal(identity);
            
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
