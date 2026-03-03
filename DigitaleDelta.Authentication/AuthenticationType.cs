// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.Authentication;

/// <summary>
/// Represents the types of authentication mechanisms supported in the system.
/// </summary>
public enum AuthenticationType
{
    None,
    OAuth2,
    OpenIdConnect,
    MTls,
    XApiKey
}
