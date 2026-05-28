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
    /// for IAuthorization (per controller, keyed) and IRequestLogger implementations.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to be configured.</param>
    /// <returns>The configured WebApplicationBuilder instance.</returns>
    public static WebApplicationBuilder RegisterRequiredCustomServices(this WebApplicationBuilder builder)
    {
        var pluginSettings = new PluginSettings();
        builder.Configuration.GetSection("PluginSettings").Bind(pluginSettings);

        var pluginLoader = new PluginLoader(pluginSettings);
        pluginLoader.LoadPlugins();

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

            Log.Information("No plugin RequestLogger found, using built-in default: {TypeName}. For custom logging, create a plugin or modify Customization/RequestLogger.cs", typeof(RequestLogger).FullName);
            return new RequestLogger(serviceProvider.GetRequiredService<IConfiguration>());
        });

        var controllerAuthorization = new Dictionary<string, ControllerAuthorisationEntry>(StringComparer.OrdinalIgnoreCase);
        builder.Configuration.GetSection("ControllerAuthorization").Bind(controllerAuthorization);

        foreach (var (controllerKey, entry) in controllerAuthorization)
        {
            var key = controllerKey;
            var configuredEntry = entry;

            builder.Services.AddKeyedScoped<IAuthorisation>(key, (serviceProvider, _) =>
            {
                if (string.Equals(configuredEntry.AuthenticationType, "None", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("Controller {Controller}: AuthenticationType=None, using default-allow Authorize", key);
                    return new Authorize(serviceProvider.GetRequiredService<IConfiguration>());
                }

                if (string.IsNullOrWhiteSpace(configuredEntry.AuthorisationHandler))
                {
                    throw new InvalidOperationException(
                        $"Controller '{key}' has AuthenticationType '{configuredEntry.AuthenticationType}' but no AuthorizationHandler configured. " +
                        "Set ControllerAuthorization:<controller>:AuthorizationHandler in appsettings.json.");
                }

                var authorization = pluginLoader.FindAndCreateInstance<IAuthorisation>(configuredEntry.AuthorisationHandler, serviceProvider);
                if (authorization == null)
                {
                    throw new InvalidOperationException(
                        $"Controller '{key}': failed to load IAuthorization implementation '{configuredEntry.AuthorisationHandler}'. " +
                        "Verify the type name and that the plugin assembly is present.");
                }

                Log.Information("Controller {Controller}: using plugin Authorization {TypeName}", key, authorization.GetType().FullName);
                return authorization;
            });
        }

        return builder;
    }
}
