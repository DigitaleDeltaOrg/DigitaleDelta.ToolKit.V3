// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Loads plugins from DLL files using reflection.
/// </summary>
public class PluginLoader(PluginSettings settings)
{
    private readonly List<Assembly> _loadedAssemblies = new();

    /// <summary>
    /// Loads all plugin assemblies from the configured plugin directory.
    /// </summary>
    public void LoadPlugins()
    {
        var pluginDirectory = Path.IsPathRooted(settings.PluginDirectory)
            ? settings.PluginDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), settings.PluginDirectory);

        if (!Directory.Exists(pluginDirectory))
        {
            Log.Warning("Plugin directory {PluginDirectory} does not exist. Creating it.", pluginDirectory);
            Directory.CreateDirectory(pluginDirectory);

            return;
        }

        var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllFile);

                _loadedAssemblies.Add(assembly);
                Log.Information("Loaded plugin assembly: {AssemblyName} from {DllFile}", assembly.FullName, dllFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load plugin assembly from {DllFile}", dllFile);
            }
        }
    }

    /// <summary>
    /// Finds and creates an instance of the specified type from configured settings or loaded plugins.
    /// </summary>
    /// <typeparam name="T">The interface type to find.</typeparam>
    /// <param name="configuredTypeName">The configured type name from settings.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>An instance of the type, or null if not found.</returns>
    public T? FindAndCreateInstance<T>(string? configuredTypeName, IServiceProvider serviceProvider) where T : class
    {
        // Try to load from configured type name first
        if (!string.IsNullOrWhiteSpace(configuredTypeName))
        {
            var instance = CreateInstanceFromTypeName<T>(configuredTypeName, serviceProvider);

            if (instance != null)
            {
                return instance;
            }
        }

        // Search in loaded plugin assemblies
        foreach (var assembly in _loadedAssemblies)
        {
            var instance = FindInstanceInAssembly<T>(assembly, serviceProvider);

            if (instance != null)
            {
                return instance;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates an instance from a fully qualified type name.
    /// </summary>
    private static T? CreateInstanceFromTypeName<T>(string typeName, IServiceProvider serviceProvider) where T : class
    {
        try
        {
            var type = Type.GetType(typeName);

            if (type != null)
            {
                return CreateInstance<T>(type, serviceProvider);
            }

            Log.Error("Could not find type {TypeName}", typeName);

            return null;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create instance of type {TypeName}", typeName);

            return null;
        }
    }

    /// <summary>
    /// Finds an instance of T in the given assembly.
    /// </summary>
    private static T? FindInstanceInAssembly<T>(Assembly assembly, IServiceProvider serviceProvider) where T : class
    {
        try
        {
            var interfaceType = typeof(T);
            var types = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && interfaceType.IsAssignableFrom(t))
                .ToList();

            switch (types.Count)
            {
                case 0:
                    return null;
                case > 1:
                    Log.Warning("Found multiple implementations of {InterfaceType} in assembly {AssemblyName}. Using the first one: {TypeName}",
                        interfaceType.Name, assembly.FullName, types[0].FullName);

                    break;
            }

            return CreateInstance<T>(types[0], serviceProvider);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to find instance of {InterfaceType} in assembly {AssemblyName}", typeof(T).Name, assembly.FullName);

            return null;
        }
    }

    /// <summary>
    /// Creates an instance of the given type with constructor dependency injection.
    /// </summary>
    private static T? CreateInstance<T>(Type type, IServiceProvider serviceProvider) where T : class
    {
        try
        {
            // Try to create instance using constructor injection
            var constructors = type.GetConstructors();

            foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                var parameterInstances = new object?[parameters.Length];
                var allResolved = true;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var service = serviceProvider.GetService(parameter.ParameterType);

                    if (service == null && !parameter.HasDefaultValue)
                    {
                        allResolved = false;
                        break;
                    }

                    parameterInstances[i] = service ?? parameter.DefaultValue;
                }

                if (!allResolved)
                {
                    continue;
                }

                var instance = constructor.Invoke(parameterInstances);

                Log.Information("Created instance of {TypeName}", type.FullName);

                return instance as T;
            }

            Log.Error("Could not resolve all constructor dependencies for type {TypeName}", type.FullName);

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create instance of type {TypeName}", type.FullName);

            return null;
        }
    }
}
