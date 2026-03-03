// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Middleware;

/// <summary>
/// Makes the responses compliant with the rules set by the API Strategy of Kennisplatform API's.
/// </summary>
public class FixKennisPlatformApiComplianceMiddleware(RequestDelegate next, IConfiguration configuration)
{
    /// <summary>
    /// Invokes the middleware to process the current HTTP request and modifies the response to comply with the rules
    /// set by the API Strategy of Kennisplatform APIs, including adding specific response headers and handling request methods.
    /// </summary>
    /// <param name="context">The current HTTP context containing request and response information.</param>
    /// <returns>A task that represents the completion of the middleware execution.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        // Add lifetime information
        context.Response.Headers.Append("API-Version", configuration.GetValue<string>("Version"));
        context.Response.Headers.Append("API-Deprecation-Date", configuration.GetValue<string>("DeprecationDate"));
        context.Response.Headers.Append("API-End-of-Life-Date", configuration.GetValue<string>("EndOfLifeDate"));
        context.Response.Headers.Append("API-Next-Release", configuration.GetValue<string>("NextMajorVersion"));
        context.Response.Headers.Append("API-Additional-Documentation", configuration.GetValue<string>("AdditionalDocumentationUrl"));

        // Add security headers (ADR /core/transport/security-headers)
        context.Response.Headers.Append("Cache-Control", "no-store");
        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // /core/http-methods: Only apply standard HTTP methods
        var allowedVerbs = new List<string> { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE" };

        if (!allowedVerbs.Contains(context.Request.Method))
        {
            context.Response.StatusCode = 405;

            return Task.CompletedTask;
        }

        // API-48: Leave off trailing slashes from URIs
        // Exception: root path "/" is allowed
        var path = context.Request.Path.ToString();

        if (!path.EndsWith('/') || path == "/")
        {
            return next.Invoke(context);
        }

        context.Response.StatusCode = 404;

        return Task.CompletedTask;
    }
}
