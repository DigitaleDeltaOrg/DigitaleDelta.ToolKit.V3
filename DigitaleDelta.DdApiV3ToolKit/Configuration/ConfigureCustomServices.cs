// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using DigitaleDelta.DdApiV3ToolKit.Customization;
using DigitaleDelta.QueryService;
using Serilog;

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Custom services configuration with plugin support.
/// </summary>
public static class CustomServicesExtensions
{
    /// <summary>
    /// Configures services for the specified WebApplicationBuilder by loading plugins
    /// for IAuthorization and IRequestLogger implementations.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to be configured.</param>
    /// <returns>The configured WebApplicationBuilder instance.</returns>
    public static WebApplicationBuilder RegisterRequiredCustomServices(this WebApplicationBuilder builder)
    {
        // Load plugin settings from configuration
        var pluginSettings = new PluginSettings();
        builder.Configuration.GetSection("PluginSettings").Bind(pluginSettings);

        // Initialize plugin loader
        var pluginLoader = new PluginLoader(pluginSettings);
        pluginLoader.LoadPlugins();

        // Register services with factory pattern to resolve plugins at runtime
        builder.Services.AddScoped<IRequestLogger>(serviceProvider =>
        {
            var logger = pluginLoader.FindAndCreateInstance<IRequestLogger>(
                pluginSettings.RequestLogger,
                serviceProvider);

            if (logger != null)
            {
                Log.Information("Using plugin RequestLogger: {TypeName}", logger.GetType().FullName);
                return logger;
            }

            // Fallback to built-in RequestLogger
            Log.Information("No plugin RequestLogger found, using built-in default: {TypeName}. For custom logging, create a plugin or modify Customization/RequestLogger.cs", typeof(RequestLogger).FullName);
            return new RequestLogger(serviceProvider.GetRequiredService<IConfiguration>());
        });

        builder.Services.AddScoped<IAuthorization>(serviceProvider =>
        {
            var authorization = pluginLoader.FindAndCreateInstance<IAuthorization>(
                pluginSettings.AuthorizationHandler,
                serviceProvider);

            if (authorization != null)
            {
                Log.Information("Using plugin Authorization: {TypeName}", authorization.GetType().FullName);
                return authorization;
            }

            // Fallback to built-in Authorize (allows all access by default)
            Log.Warning("No plugin Authorization found, using built-in default: {TypeName} (allows ALL access). For production use, create a plugin or modify Customization/Authorize.cs", typeof(Authorize).FullName);
            return new Authorize(serviceProvider.GetRequiredService<IConfiguration>());
        });

        return builder;
    }
}