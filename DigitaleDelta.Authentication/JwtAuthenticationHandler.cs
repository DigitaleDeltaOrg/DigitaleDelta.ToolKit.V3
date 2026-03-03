// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Represents an authentication handler that validates and authenticates JWT tokens.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IAuthenticationHandler"/> interface to provide
/// functionality for authenticating HTTP requests using JWT (JSON Web Token) bearer tokens.
/// The tokens are validated using the provided <see cref="TokenValidationParameters"/>.
/// </remarks>
public class JwtAuthenticationHandler(TokenValidationParameters validationParameters) : IAuthenticationHandler
{
    /// <summary>
    /// Attempts to authenticate the incoming HTTP request using a JWT token provided in the
    /// Authorization header. If the authentication is successful, a claims principal is created
    /// and associated with the HTTP context.
    /// </summary>
    /// <param name="context">The HttpContext containing the request and response information.</param>
    /// <param name="principal">When this method returns, contains the authenticated ClaimsPrincipal if authentication
    /// is successful; otherwise, null.</param>
    /// <returns>A Task containing a boolean value. Returns true if authentication succeeds; otherwise, false.</returns>
    public Task<bool> TryAuthenticateAsync(HttpContext context, out ClaimsPrincipal? principal)
    {
        principal = null;

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (authHeader == null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            principal = jwtSecurityTokenHandler.ValidateToken(token, validationParameters, out _);
            
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }
}