// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using DigitaleDelta.QueryService;

namespace DigitaleDelta.DdApiV3ToolKit.Customization;

#pragma warning disable CS9113 // Parameter is unread.

/// <summary>
/// Implement authorisation based on the identity of the requester, in the form of a ClaimsPrincipal.
/// </summary>
public class Authorize(IConfiguration configuration) : IAuthorization
{
    /// <summary>
    /// Implement authorisation logic. ClaimsPrincipal contains the identity of the requester. Configuration can be used to read any settings needed.
    /// The access string will replace the @access placeholder in the SQL Templates, when authorised is true. By default, allow all access.
    /// </summary>
    /// <param name="claimsPrincipal"></param>
    /// <returns></returns>
    public Task<(bool authorised, string access)> TryAuthorizeAsync(ClaimsPrincipal? claimsPrincipal)
    {
        // Implement your own logic.
        return Task.FromResult((true, " 1 = 1 ")); // Allow all access.
    }
}
