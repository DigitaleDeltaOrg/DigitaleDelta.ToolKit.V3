// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Claims;

namespace DigitaleDelta.DdApiV3ToolKit.Middleware;

/// <summary>
/// Middleware for facilitating development authentication.
/// This middleware is specifically intended for debugging and testing purposes.
/// It acts as a placeholder for authentication by creating a mock authenticated user
/// when certain conditions are met, such as the absence of an actual authentication token
/// and the presence of an attached debugger.
/// </summary>
public class DevAuthenticationMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware for processing HTTP requests and conditionally
    /// adds a development authentication user when certain criteria are met.
    /// This is specifically intended for development scenarios where a debugger
    /// is attached, and no actual authentication token is present.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!Debugger.IsAttached)
        {
            await next(context).ConfigureAwait(false);
        }

        // Als al geauthenticeerd: niets doen.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // Als er een Authorization header is: normale auth laten afhandelen.
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // Fake alleen als debugger is aangesloten.
        if (!Debugger.IsAttached)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // Optioneel: laat een custom AppId via header toe voor testen.
        var appId = context.Request.Headers.TryGetValue("X-Dev-AppId", out var v)
            ? v.ToString()
            : "Debug";

        var claims = new List<Claim>
        {
            // v2: authorised party (Client ID)
            new("azp", appId),
            // v1: appid (Client ID)
            new("appid", appId)
        };

        var identity  = new ClaimsIdentity(claims, "Development");
        var principal = new ClaimsPrincipal(identity);

        context.User = principal;

        // Belangrijk: pipeline doorlaten.
        await next(context).ConfigureAwait(false);
    }
}
