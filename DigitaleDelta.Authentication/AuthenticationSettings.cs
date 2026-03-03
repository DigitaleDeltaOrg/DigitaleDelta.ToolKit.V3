// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace DigitaleDelta.Authentication;

/// <summary>
/// Provides configuration settings for managing authentication in a system.
/// </summary>
public class AuthenticationSettings
{
    /// <summary>
    /// Gets the type of authentication mechanism to be used.
    /// This property specifies the authentication type for the system, such as None, OAuth2, OpenIDConnect, MTLS, or XApiKey.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthenticationType Type { get; init; }

    /// <summary>
    /// Gets the authority or issuer URL used for identity validation during authentication.
    /// This property identifies the trusted authority, such as an Identity Provider (IdP),
    /// which issues tokens or credentials for the system.
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>
    /// Gets the identifier of the target audience for token validation.
    /// This property is used to specify the intended recipient of the token, ensuring that tokens are consumed only by trusted parties.
    /// </summary>
    public string? Audience { get; init; }

    /// <summary>
    /// Gets the name of the header used for the API key in authentication.
    /// This property specifies the header field where the API key should be provided
    /// when using XApiKey as the authentication type.
    /// </summary>
    public string? ApiKeyHeader { get; init; }

    /// <summary>
    /// Specifies the API key value that is permitted for authentication.
    /// This property defines the valid API key expected when the authentication
    /// method is set to XApiKey.
    /// </summary>
    public string? AllowedApiKey { get; init; }
}
