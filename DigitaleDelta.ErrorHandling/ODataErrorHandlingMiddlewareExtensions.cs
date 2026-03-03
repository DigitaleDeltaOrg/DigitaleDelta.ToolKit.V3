// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace DigitaleDelta.ErrorHandling;

/// <summary>
/// Extension methods for registering OData error handling middleware.
/// </summary>
public static class ODataErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the OData error handling middleware to the application's request pipeline.
    /// </summary>
    /// <param name="app">The IApplicationBuilder instance.</param>
    /// <returns>The IApplicationBuilder for chaining.</returns>
    public static IApplicationBuilder UseODataErrorHandling(this IApplicationBuilder app) => app.UseMiddleware<ODataErrorHandlingMiddleware>();
}