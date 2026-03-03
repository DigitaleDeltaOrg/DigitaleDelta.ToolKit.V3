// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Text.Json.Serialization;
using DigitaleDelta.Authentication;
using DigitaleDelta.Contracts.Configuration;
using DigitaleDelta.DdApiV3ToolKit.Middleware;
using DigitaleDelta.ErrorHandling;
using DigitaleDelta.ODataTranslator;
using DigitaleDelta.ODataWriter;
using DigitaleDelta.RequestHelpers;
using Markdig;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Serilog;

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Contains extension methods for configuring services and the application pipeline
/// </summary>
public static class Configure
{
    public const string configurationFilesPath = "ConfigurationFiles";

    /// <summary>
    /// Configures services for the specified WebApplicationBuilder by adding necessary middleware, caching, and API behaviour,
    /// controller settings, rate limiting, CORS policies, and other dependencies.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to be configured.</param>
    /// <returns>The configured WebApplicationBuilder instance.</returns>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.ConfigureLogging();

        var databaseConnectionFactory = DatabaseConnectionFactory(builder.Configuration);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var baseParameters = new BaseParameters
        {
            ParameterPrefix = builder.Configuration.GetValue<char?>("ParameterPrefix") ?? '@'
        };

        // Some parameters will not be picked up with a hot reload, but those will not change often.
        // For query changes, always start the app again instead of relying on hot reload.
        builder.Services.AddKeyedSingleton("ObservationQueryServiceParameters", builder.GetQueryServiceParameters("ObservationConfiguration", "observations", databaseConnectionFactory));
        builder.Services.AddKeyedSingleton("ReferenceQueryServiceParameters", builder.GetQueryServiceParameters("ReferenceConfiguration", "observations", databaseConnectionFactory));
        builder.Services.ConfigureRateLimiting(builder.Configuration);
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            // Request size limits (from Kestrel:Limits in appsettings.json)
            options.Limits.MaxRequestBodySize = builder.Configuration.GetValue<long?>("Kestrel:Limits:MaxRequestBodySize") ?? 1024 * 1024 * 1024;
            options.Limits.MaxRequestHeaderCount = builder.Configuration.GetValue<int?>("Kestrel:Limits:MaxRequestHeaderCount") ?? 100;
            options.Limits.MaxRequestHeadersTotalSize = builder.Configuration.GetValue<int?>("Kestrel:Limits:MaxRequestHeadersTotalSize") ?? 32 * 1024;
            options.Limits.MaxRequestLineSize = builder.Configuration.GetValue<int?>("Kestrel:Limits:MaxRequestLineSize") ?? 8 * 1024;

            // Connection limits
            options.Limits.MaxConcurrentConnections = null; // null = unlimited (controlled by rate limiting)
            options.Limits.MaxConcurrentUpgradedConnections = null;
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("Kestrel:Limits:KeepAliveTimeoutMinutes") ?? 2);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int?>("Kestrel:Limits:RequestHeadersTimeoutSeconds") ?? 30);

            // Security
            options.AddServerHeader = false; // Suppress Server header for security
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IConfiguration>(_ => builder.Configuration);
        builder.Services.AddSingleton<IMemoryCache>(cache);
        builder.Services.AddSingleton(baseParameters);
        builder.Services.AddSingleton(new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
        builder.Services.AddOptions();
        builder.Services.AddMemoryCache();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddRequestDecompression();
        builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
        builder.Services.AddDigitaleDeltaAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddCors(CorsOptions(builder.Configuration));
        builder.Services.AddControllers(ControllerOptions()).AddJsonOptions(JsonOptions());

        return builder;
    }

    /// <summary>
    /// Configures the specified WebApplication by adding middleware, routing, CORS policies, exception handling, authentication,
    /// and other application setup functionality.
    /// </summary>
    /// <param name="application">The WebApplication instance to be configured.</param>
    /// <returns>The configured WebApplication instance.</returns>
    public static WebApplication ConfigureApplication(this WebApplication application)
    {
        application.UseCors("Cors");
        application.UseODataErrorHandling(); // Middleware for handling OData errors
        application.UseExceptionHandler();
        application.UseDeveloperExceptionPage();
        application.UseMiddleware<RequestExtensions.QueryToHeaderMiddleware>();
        application.UseRouting();
        application.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true, DefaultContentType  = "application/xml" });
        application.UseMiddleware<DevAuthenticationMiddleware>(); // Only runs when the debugger is attached.
        application.UseMiddleware<AuthenticationMiddleware>();
        application.UseAuthentication();
        application.UseAuthorization();
        application.UseRequestDecompression();
        application.UseMiddleware<JsonToYamlMiddleware>();
        application.UseMiddleware<FixKennisPlatformApiComplianceMiddleware>();

        var appFileContents = application.Services.GetRequiredService<AppFileContents>();

        ConfigureStaticFileEndpoints(application, appFileContents);

        application.MapControllers();

        application.Lifetime.ApplicationStarted.Register(() =>
        {
            var addresses = application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;

            Log.Logger.Information("Application started. Listening on: {Addresses}", string.Join(", ", addresses ?? []));
        });

        return application;
    }

    /// <summary>
    /// Configures static file endpoints for the specified WebApplication to serve various static resources,
    /// including OpenAPI specifications, metadata documents, and HTML pages.
    /// </summary>
    /// <param name="application">The WebApplication instance to configure static file endpoints for.</param>
    /// <param name="appFileContents">The AppFileContents instance containing necessary file contents like OpenAPI and SVG data.</param>
    private static void ConfigureStaticFileEndpoints(WebApplication application, AppFileContents appFileContents)
    {
        // Serve static files, which are based on templates.
        application.MapGet("/wwwroot/DigitaleDelta.svg", async context => { context.Response.ContentType = "image/svg+xml"; await context.Response.WriteAsync(appFileContents.SvgText); });
        application.MapGet("/v3/DigitaleDelta.svg", async context => { context.Response.ContentType = "image/svg+xml"; await context.Response.WriteAsync(appFileContents.SvgText); });
        application.MapGet("/v3/odata/$metadata", async context => { context.Response.ContentType = "application/xml; charset=utf-8"; await context.Response.WriteAsync(appFileContents.CsdlText); });
        application.MapGet("/v3/openapi.yaml", async context => { context.Response.ContentType = "application/yaml"; await context.Response.WriteAsync(appFileContents.OpenApiText); });
        application.MapGet("/v3/openapi.json", async context => await SetResponseHeadersAndWriteJson(context, appFileContents));
        application.MapGet("/v3/openapi", async context => await SetResponseHeadersAndWriteJson(context, appFileContents));
        application.MapGet("/v3/", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "index.html")));
        application.MapGet("/v3/index.html", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "index.html")));
        application.MapGet("/v3/swagger", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "swagger.html")));
        application.MapGet("/v3/redoc", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "redoc.html")));
        application.MapGet("/v3/swagger.html", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "swagger.html")));
        application.MapGet("/v3/redoc.html", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "redoc.html")));
        application.MapGet("/", async context => await SendFile(context, "text/html", Path.Combine(configurationFilesPath, "default-index.html")));
        application.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();
    }

    /// <summary>
    /// Sends a file to the client's response stream with the specified content type.
    /// </summary>
    /// <param name="context">The HttpContext instance representing the current HTTP request and response context.</param>
    /// <param name="format">The MIME type to be used as the Content-Type header for the response.</param>
    /// <param name="filePath">The path to the file that will be sent as the response body.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private static async Task SendFile(HttpContext context, string format, string filePath)
    {
        context.Response.ContentType = format;
        await context.Response.SendFileAsync(filePath);
    }

    /// <summary>
    /// Sets response headers and writes JSON content to the HTTP response using the provided OpenAPI text.
    /// </summary>
    /// <param name="context">The HTTP context containing the response where content will be written.</param>
    /// <param name="appFileContents">The AppFileContents instance containing the OpenAPI text to be converted to JSON and written to the response.</param>
    /// <returns>A task that represents the asynchronous operation of setting response headers and writing JSON content.</returns>
    private static async Task SetResponseHeadersAndWriteJson(HttpContext context, AppFileContents appFileContents)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        await context.Response.WriteAsync(YamlJsonConverter.ConvertYamlToJson(appFileContents.OpenApiText));
    }

    /// <summary>
    /// Reads configuration files and environment variables into the WebApplicationBuilder's configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ReadConfigurationFiles(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(Path.Combine(configurationFilesPath, "appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(configurationFilesPath, "appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine(configurationFilesPath, "observation-configuration.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(configurationFilesPath, "reference-configuration.json"), optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder;
    }

    /// <summary>
    /// Creates a factory method for generating database connections based on the specified database engine and connection string.
    /// </summary>
    /// <returns>A function that initialises and returns a database connection.</returns>
    /// <exception cref="Exception">Thrown when the specified database engine is not supported.</exception>
    internal static Func<DbConnection> DatabaseConnectionFactory(IConfiguration configuration)
    {
        return ConnectionFactory;

        DbConnection ConnectionFactory()
        {
            var databaseEngine = configuration.GetValue<string>("DatabaseEngine");
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            return databaseEngine switch
            {
                "SqlServer" => new SqlConnection(connectionString),
                "Postgres" => new NpgsqlConnection(connectionString),
                "MySql" => new MySqlConnection(connectionString),
                "Oracle" => new OracleConnection(connectionString),
                "Sqlite" => new SqliteConnection(connectionString),
                _ => throw new Exception($"Database engine {databaseEngine} is not supported. Supported: SqlServer, Postgres, MySql, Oracle, Sqlite.")
            };
        }
    }

    /// <summary>
    /// Configures CORS policies based on the application configuration.
    /// Supports either wildcard (*) for any origin or a comma-separated list of specific allowed origins.
    /// </summary>
    /// <param name="configuration">The application configuration containing CORS settings.</param>
    /// <returns>An action to configure CORS options with the specified policy.</returns>
    private static Action<CorsOptions> CorsOptions(IConfiguration configuration)
    {
        return options =>
        {
            options.AddPolicy("Cors", policy =>
            {
                var allowedOrigins = configuration.GetValue<string>("Cors:AllowedOrigins");

                if (string.IsNullOrWhiteSpace(allowedOrigins) || allowedOrigins == "*")
                {
                    // Allow any origin (public API)
                    policy.AllowAnyOrigin();
                }
                else
                {
                    // Allow specific origins from a comma-separated list
                    var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    policy.WithOrigins(origins);
                }

                policy.AllowAnyMethod().AllowAnyHeader();
            });
        };
    }

    /// <summary>
    /// Configures JSON serialization options for the application, including settings for property naming,
    /// ignoring null values, handling named floating-point literals, and custom converters for serialization needs.
    /// </summary>
    /// <returns>An action delegate that modifies the JSON serialization settings.</returns>
    private static Action<JsonOptions> JsonOptions()
    {
        return options =>
        {
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.JsonSerializerOptions.Converters.Add(new SlashPathExpandingDictionaryConverter());
        };
    }

    /// <summary>
    /// Creates and configures an instance of <c>ODataQueryServiceParameters</c> based on the provided configuration and database connection settings.
    /// </summary>
    /// <param name="builder">The <c>WebApplicationBuilder</c> containing the application configuration.</param>
    /// <param name="key">The configuration key prefix used to locate specific query service parameters.</param>
    /// <param name="setName">The entity set name for which the query service parameters are being created.</param>
    /// <param name="databaseConnectionFactory">A factory function to create database connection instances.</param>
    /// <returns>An initialised instance of <c>ODataQueryServiceParameters</c>.</returns>
    /// <exception cref="Exception">Thrown when required property mappings or function mappings are not properly configured.</exception>
    private static ODataQueryServiceParameters GetQueryServiceParameters(this WebApplicationBuilder builder, string key, string setName, Func<DbConnection> databaseConnectionFactory)
    {
        var propertyMapping = builder.Configuration.GetSection($"{key}:PropertyMapping").Get<List<ODataToSqlMap>>();
        var functionMapping = builder.Configuration.GetSection($"{key}:FunctionMapping").Get<List<ODataFunctionMap>>();
        var dataQueryFile = builder.Configuration.GetValue<string>($"{key}:DataQueryFile") ?? string.Empty;
        var countQueryFile = builder.Configuration.GetValue<string>($"{key}:CountQueryFile") ?? string.Empty;

        if (propertyMapping == null || propertyMapping.Count == 0)
        {
            throw new Exception($"No property mapping found for {key}.");
        }

        if (functionMapping == null || functionMapping.Count == 0)
        {
            throw new Exception($"No function mapping found for {key}.");
        }

        return new ODataQueryServiceParameters
        {
            PropertyMap = propertyMapping.ToDictionary(a => a.ODataPropertyName, StringComparer.OrdinalIgnoreCase),
            FunctionMap = functionMapping.ToDictionary(a => a.ODataFunctionName, StringComparer.OrdinalIgnoreCase),
            ConnectionFactory = databaseConnectionFactory,
            EntitySetName = setName,
            DataQuery = LoadFileContent(dataQueryFile),
            CountQuery = LoadFileContent(countQueryFile),
            IdField = builder.Configuration.GetValue<string>($"{key}:IdField") ?? string.Empty,
            TokenSecret = builder.Configuration.GetValue<string>("TokenSecret") ?? string.Empty,
            CountCachePeriod = builder.Configuration.GetValue<int?>("CountCachePeriod") ?? 5,
            HmacAlgorithm = builder.Configuration.GetValue<string>("HmacAlgorithm") ?? "HMACSHA256",
            MaxPageSize = builder.Configuration.GetValue<int?>($"{key}:MaxPageSize") ?? 10000,
            DefaultPageSize = builder.Configuration.GetValue<int?>($"{key}:MaxPageSize") ?? 1000
        };
    }

    /// <summary>
    /// Configures the options for ASP.NET Core MVC controllers by adding custom metadata providers
    /// and filters to enhance behaviour such as model validation and rate limiting.
    /// </summary>
    /// <returns>An action that modifies the MVC options for controller configuration.</returns>
    private static Action<MvcOptions> ControllerOptions()
    {
        return options =>
        {
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        };
    }

    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Services.AddSerilog();

        return builder;
    }

    /// <summary>
    /// Loads the content of a query file specified by its name from the Queries directory.
    /// </summary>
    /// <param name="fileName">The name of the query file to be loaded, including its extension.</param>
    /// <returns>The content of the query file as a string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the queryName or application base directory is null.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the query file does not exist in the Queries directory.</exception>
    private static string LoadFileContent(string? fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(Directory.GetCurrentDirectory());

        var file = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        return File.Exists(file)
            ? File.ReadAllText(file)
            : throw new FileNotFoundException($"Bestand '{file}' niet gevonden.");
    }
}
