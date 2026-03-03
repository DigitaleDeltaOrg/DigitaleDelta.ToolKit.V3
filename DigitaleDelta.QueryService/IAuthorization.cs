// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace DigitaleDelta.QueryService;

/// <summary>
/// Interface for implementing authorisation logic.
/// </summary>
public interface IAuthorization
{
    /// <summary>
    /// Attempt to authorise the given ClaimsPrincipal.
    /// </summary>
    /// <param name="claimsPrincipal">Principal to authenticate.</param>
    /// <returns>Tuple containing authorisation status and access string.</returns>
    Task<(bool authorised, string access)> TryAuthorizeAsync(ClaimsPrincipal? claimsPrincipal);
}
