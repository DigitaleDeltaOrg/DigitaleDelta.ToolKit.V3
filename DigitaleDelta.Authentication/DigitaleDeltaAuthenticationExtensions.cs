// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Provides extension methods for configuring Digitale Delta authentication with the dependency injection container.
/// </summary>
public static class DigitaleDeltaAuthenticationExtensions
{
    /// <summary>
    /// Adds Digitale Delta authentication services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which the authentication services will be added.</param>
    /// <param name="config">The configuration object containing authentication settings.</param>
    /// <returns>The updated service collection with the authentication services configured.</returns>
    public static IServiceCollection AddDigitaleDeltaAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AuthenticationSettings>(config.GetSection("Authentication"));
        services.AddSingleton<AuthenticationHandlerFactory>();
        services.AddTransient<NoAuthenticationHandler>();
        services.AddTransient<ApiKeyAuthenticationHandler>(_ =>
        {
            var headerName = config["Authentication:HeaderName"] ?? "X-API-KEY";
            return new ApiKeyAuthenticationHandler(headerName);
        });
        services.AddTransient<JwtAuthenticationHandler>(_ =>
        {
            var tokenValidationParameters = new TokenValidationParameters();
            return new JwtAuthenticationHandler(tokenValidationParameters);
        });
        services.AddTransient<MTlsAuthenticationHandler>();

        return services;
    }
}
