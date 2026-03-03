// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Handles mutual TLS (mTLS) authentication for establishing the identity of a client
/// based on the provided client certificate within an HTTP context.
/// </summary>
public class MTlsAuthenticationHandler : IAuthenticationHandler
{
    /// <summary>
    /// Attempts to authenticate an incoming request using mutual TLS (mTLS).
    /// Verifies the presence of a client certificate and, if valid, creates a
    /// <see cref="ClaimsPrincipal"/> with relevant claims extracted from the certificate.
    /// </summary>
    /// <param name="context">The current HTTP request context, used to access connection and client certificate details.</param>
    /// <param name="principal">
    /// When the method returns, contains the <see cref="ClaimsPrincipal"/> representing the authenticated user,
    /// or <c>null</c> if authentication fails.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// The task result contains a boolean value, indicating whether authentication was successful (<c>true</c>) or not (<c>false</c>).
    /// </returns>
    public Task<bool> TryAuthenticateAsync(HttpContext context, out ClaimsPrincipal? principal)
    {
        principal = null;
        
        var cert = context.Connection.ClientCertificate;
        
        if (cert == null)
        {
            return Task.FromResult(false);
        }

        var claims = new List<Claim>
        {
            new("x509.subject", cert.Subject),
            new("x509.thumbprint", cert.Thumbprint),
            new("x509.issuer", cert.Issuer),
            new("x509.notbefore", cert.NotBefore.ToString("O")),
            new("x509.notafter", cert.NotAfter.ToString("O"))
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "mTLS");
        principal = new ClaimsPrincipal(identity);
        
        return Task.FromResult(true);
    }
}