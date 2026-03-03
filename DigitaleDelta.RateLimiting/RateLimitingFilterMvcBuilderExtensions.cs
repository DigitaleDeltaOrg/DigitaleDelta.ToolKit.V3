// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DigitaleDelta.RateLimiting;

/// <summary>
/// Provides extension methods for adding and configuring rate-limiting filters in the MVC pipeline.
/// </summary>
public static class RateLimitingFilterMvcBuilderExtensions
{
    /// <summary>
    /// Voegt de RateLimitingFilter als globale filter toe aan de MVC pipeline en bindt de opties.
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IMvcBuilder AddRateLimitingFilter(this IMvcBuilder mvcBuilder, IConfiguration configuration)
    {
        mvcBuilder.Services.Configure<Dictionary<string, RateLimitOptions>>(configuration.GetSection("RateLimits"));
        mvcBuilder.Services.AddScoped<RateLimitingFilter>();
        mvcBuilder.AddMvcOptions(options =>
        {
            options.Filters.Add<RateLimitingFilter>();
        });
        
        return mvcBuilder;
    }
}