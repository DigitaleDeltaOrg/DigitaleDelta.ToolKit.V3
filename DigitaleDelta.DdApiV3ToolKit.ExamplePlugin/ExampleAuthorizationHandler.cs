// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using DigitaleDelta.QueryService;
using Microsoft.Extensions.Configuration;

namespace DigitaleDelta.DdApiV3ToolKit.ExamplePlugin;

/// <summary>
/// Example implementation of IAuthorization that allows all access.
/// In production, implement your own authorisation logic based on claims, roles, database checks, etc.
/// </summary>
public class ExampleAuthorizationHandler : IAuthorization
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor with dependency injection support.
    /// You can inject any service registered in the application's DI container.
    /// </summary>
    public ExampleAuthorizationHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Example authorisation logic - customise this based on your requirements. The result is a tuple indicating if authorised and the SQL access condition.
    /// The SQL access condition is a string that will be included in the WHERE clause of the generated SQL query to filter data based on the user's permissions.
    /// </summary>
    /// <param name="claimsPrincipal">The claims principal containing user identity information.</param>
    /// <returns>A tuple indicating if authorised and the SQL access condition.</returns>
    public Task<(bool authorised, string access)> TryAuthorizeAsync(ClaimsPrincipal? claimsPrincipal)
    {
        // Example: Check if user is authenticated
        if (claimsPrincipal?.Identity?.IsAuthenticated == true)
        {
            // Example: Get user's organisation from claims
            var organizationClaim = claimsPrincipal.FindFirst("organization")?.Value;

            if (!string.IsNullOrEmpty(organizationClaim))
            {
                // Return SQL condition that filters data based on organisation
                return Task.FromResult((true, $"organization = '{organizationClaim}'"));
            }
        }

        // Default: Allow all access (change this in production!)
        // Return "1 = 1" to allow all, or "1 = 0" to deny all
        return Task.FromResult((true, "1 = 1"));
    }
}
