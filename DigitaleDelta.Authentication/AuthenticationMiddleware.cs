// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Middleware responsible for handling authentication of incoming requests.
/// </summary>
/// <remarks>
/// The middleware determines the authentication mechanism based on the configuration provided
/// in <see cref="AuthenticationSettings"/> and utilizes the appropriate handler from
/// <see cref="AuthenticationHandlerFactory"/> to authenticate the request. When the matched
/// controller is configured with <c>ControllerAuthorization:&lt;ControllerName&gt;:AuthenticationType = "None"</c>,
/// the middleware passes the request through without enforcing authentication.
/// </remarks>
/// <param name="next">The next middleware in the request pipeline.</param>
/// <param name="factory">
/// A factory for generating authentication handlers based on the configured <see cref="AuthenticationType"/>.
/// </param>
/// <param name="settings">Configuration settings defining the authentication mechanism to use.</param>
/// <param name="configuration">Application configuration, used to look up per-controller authorisation overrides.</param>
public class AuthenticationMiddleware(RequestDelegate next, AuthenticationHandlerFactory factory, IOptions<AuthenticationSettings> settings, IConfiguration configuration)
{
    private readonly AuthenticationSettings _settings = settings.Value;

    /// <summary>
    /// Processes the HTTP request within the middleware and handles authentication.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> representing the current HTTP request.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null || IsControllerAnonymous(endpoint))
        {
            await next(context);

            return;
        }

        var handler = factory.Create(_settings.Type);

        if (!await handler.TryAuthenticateAsync(context, out var principal).ConfigureAwait(false))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return;
        }

        context.User = principal ?? new ClaimsPrincipal();

        await next(context);
    }

    private bool IsControllerAnonymous(Endpoint? endpoint)
    {
        var controllerName = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.Name;

        if (string.IsNullOrEmpty(controllerName))
        {
            return false;
        }

        var configured = configuration[$"ControllerAuthorization:{controllerName}:AuthenticationType"];

        return string.Equals(configured, "None", StringComparison.OrdinalIgnoreCase);
    }
}
