// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Factory for creating instances of <see cref="IAuthenticationHandler"/> based on the specified
/// <see cref="AuthenticationType"/>.
/// </summary>
/// <remarks>
/// This factory leverages the registered services in the provided <see cref="IServiceProvider"/> to resolve
/// and instantiate the appropriate authentication handler implementation.
/// </remarks>
/// <example>
/// The factory supports the following <see cref="AuthenticationType"/> values:
/// - None: Resolves to <see cref="NoAuthenticationHandler"/>.
/// - XApiKey: Resolves to <see cref="ApiKeyAuthenticationHandler"/>.
/// - OAuth2, OpenIDConnect: Resolve to <see cref="JwtAuthenticationHandler"/>.
/// - MTls: Resolves to <see cref="MTlsAuthenticationHandler"/>.
/// An exception of type <see cref="NotSupportedException"/> is thrown if an unsupported <see cref="AuthenticationType"/> is provided.
/// </example>
/// <param name="sp">An <see cref="IServiceProvider"/> used to resolve the authentication handler instances.</param>
public class AuthenticationHandlerFactory(IServiceProvider sp)
{
    /// <summary>
    /// Creates an instance of the appropriate authentication handler based on the specified authentication type.
    /// </summary>
    /// <param name="type">The authentication type for which the handler is to be created.</param>
    /// <returns>An implementation of <see cref="IAuthenticationHandler"/> corresponding to the specified authentication type.</returns>
    /// <exception cref="NotSupportedException">Thrown if the specified authentication type is not supported.</exception>
    public IAuthenticationHandler Create(AuthenticationType type) => type switch
    {
        AuthenticationType.None => sp.GetRequiredService<NoAuthenticationHandler>(),
        AuthenticationType.XApiKey => sp.GetRequiredService<ApiKeyAuthenticationHandler>(),
        AuthenticationType.OAuth2 => sp.GetRequiredService<JwtAuthenticationHandler>(),
        AuthenticationType.OpenIdConnect => sp.GetRequiredService<JwtAuthenticationHandler>(),
        AuthenticationType.MTls => sp.GetRequiredService<MTlsAuthenticationHandler>(),
        _ => throw new NotSupportedException()
    };
}