// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DigitaleDelta.Authentication;
using DigitaleDelta.Contracts;
using DigitaleDelta.Contracts.Configuration;
using Serilog;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace DigitaleDelta.DdApiV3ToolKit.Configuration;

/// <summary>
/// Validates the application's configuration by ensuring that all required sections are present and valid.
/// </summary>
public static class ConfigurationValidator
{
    private static bool _isValid = true;
    private static CsdlModel? _csdlModel;
    private static readonly string _csdlFileName = Path.Combine(Configure.configurationFilesPath, "csdl.xml");
    private static readonly string _openApiFileName = Path.Combine(Configure.configurationFilesPath, "openapi.yaml");
    private static readonly string _svgFileName = Path.Combine(Configure.configurationFilesPath, "logo-v3.svg");
    private static readonly string _indexHtmlFileName = Path.Combine(Configure.configurationFilesPath, "index.html");
    private static readonly string _swaggerHtmlFileName = Path.Combine(Configure.configurationFilesPath, "swagger.html");
    private static readonly string _redocHtmlFileName = Path.Combine(Configure.configurationFilesPath, "redoc.html");
    private static Dictionary<string, DigitaleDeltaDefinition> _contextDefinitions = new(StringComparer.InvariantCultureIgnoreCase);
    private static Dictionary<string, DigitaleDeltaDefinition> _metadataDefinitions = new(StringComparer.InvariantCultureIgnoreCase);
    private const string parameterPrefix = "parameter/";
    private const string metadataPrefix = "metadata/";
    private const string contextDefinitionsName = "ContextDefinitions";
    private const string metadataDefinitionsName = "MetadataDefinitions";
    private const string observationConfiguration = "ObservationConfiguration";
    private const string referenceConfiguration = "ReferenceConfiguration";
    private const string propertyMapping = "PropertyMapping";
    private const string functionMapping = "FunctionMapping";
    private const string rateLimitOptions = "RateLimitOptions";
    private static readonly List<string> _requiredProperties =
    [
        "Id",
        "Type",
        "ValidTime/BeginPosition",
        "PhenomenonTime/BeginPosition",
        "PhenomenonTime/EndPosition",
        "ResultTime",
        "Foi/Id",
        "Foi/Code",
        "Foi/Name",
        "Foi/Geography",
        "Parameter/Organisation",
        "Parameter/OrganisationNamespace",
        "Parameter/Compartment",
        "Metadata/ModifiedOn"
    ];

    /// <param name="builder">The host application builder that contains the application's configuration.</param>
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Validates the application's configuration by ensuring that all required sections are present and valid.
        /// </summary>
        /// <exception cref="Exception">Thrown if any required configuration section is missing or invalid.</exception>
        public WebApplicationBuilder ValidateConfiguration()
        {
            ValidateFileExists("CsdlFileName", _csdlFileName);
            ValidateFileExists("OpenApiFileName", _openApiFileName);
            ValidateFileExists("SvgFileName", _svgFileName);
            ValidateFileExists("IndexHtmlFileName", _indexHtmlFileName);
            ValidateFileExists("SwaggerHtmlFileName", _swaggerHtmlFileName);
            ValidateFileExists("RedocHtmlFileName", _redocHtmlFileName);
            ValidateFileExists($"{observationConfiguration}:DataQueryFile", builder.Configuration.GetValue<string?>($"{observationConfiguration}:DataQueryFile"));
            ValidateFileExists($"{observationConfiguration}:CountQueryFile", builder.Configuration.GetValue<string?>($"{observationConfiguration}:CountQueryFile"));
            ValidateFileExists($"{referenceConfiguration}:DataQueryFile", builder.Configuration.GetValue<string?>($"{referenceConfiguration}:DataQueryFile"));
            ValidateFileExists($"{referenceConfiguration}:CountQueryFile", builder.Configuration.GetValue<string?>($"{referenceConfiguration}:CountQueryFile"));
            ValidateConnectionData(builder.Configuration);
            ValidateStringWithMinimalLength(builder.Configuration, "TokenSecret", 32);
            ValidateTokenAlgorithm(builder.Configuration);
            ValidateAuthenticationConfiguration(builder.Configuration);
            ValidateSection(builder.Configuration, referenceConfiguration);
            ValidateSection(builder.Configuration, observationConfiguration);
            _contextDefinitions = GetDefinitions(builder.Configuration.GetValue<string>(contextDefinitionsName), contextDefinitionsName);
            _metadataDefinitions = GetDefinitions(builder.Configuration.GetValue<string>(metadataDefinitionsName), metadataDefinitionsName);
            ValidateDefinitions(builder.Configuration, _contextDefinitions, parameterPrefix, $"{observationConfiguration}:{propertyMapping}");
            ValidateDefinitions(builder.Configuration, _metadataDefinitions, metadataPrefix, $"{observationConfiguration}:{propertyMapping}");
            ValidateRequiredDefinitions(builder.Configuration);

            return _isValid ? builder : throw new Exception("Configuratie validatie mislukt. Zie bovenstaande fouten voor details.");
        }

        /// <summary>
        /// Compiles the CSDL and OpenAPI definitions and a customised SVG based on the configuration and registers them as services.
        /// </summary>
        /// <returns></returns>
        public WebApplicationBuilder CompileDefinitions()
        {
            const string propertyMappingKey = $"{observationConfiguration}:{propertyMapping}";
            var parameterPropertiesInConfiguration = GetPropertyMaps(builder.Configuration, propertyMappingKey)
                .Where(a => a.ODataPropertyName.StartsWith(parameterPrefix, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(a => a.ODataPropertyName).ToList();
            var metadataPropertiesInConfiguration = GetPropertyMaps(builder.Configuration, propertyMappingKey)
                .Where(a => a.ODataPropertyName.StartsWith(metadataPrefix, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(a => a.ODataPropertyName).ToList();
            var csdlText = File.ReadAllText(_csdlFileName);
            var openApiText = File.ReadAllText(_openApiFileName);
            var svgText = File.ReadAllText(_svgFileName);

            csdlText = csdlText.Replace("<!-- ParameterPlaceholder -->", string.Join(Environment.NewLine, parameterPropertiesInConfiguration.OrderBy(a => a.ODataPropertyName).Select(a => $"           <Property Name=\"{a.ODataPropertyName.Replace(parameterPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase)}\" Type=\"{a.EdmType}\" Nullable=\"true\" />")));
            csdlText = csdlText.Replace("<!-- MetadataPlaceholder -->", string.Join(Environment.NewLine, metadataPropertiesInConfiguration.OrderBy(a => a.ODataPropertyName).Select(a => $"             <Property Name=\"{a.ODataPropertyName.Replace(metadataPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase)}\" Type=\"{a.EdmType}\" Nullable=\"true\" />")));

            if (!CsdlParser.CsdlParser.TryParse(csdlText, out _csdlModel, out var error) || _csdlModel == null)
            {
                LogError($"CSDL parser error after compilation: {error}");
                Environment.Exit(1);
            }

            builder.Services.AddSingleton(_csdlModel);

            var parameterProperties = string.Join(Environment.NewLine, parameterPropertiesInConfiguration.OrderBy(a => a.ODataPropertyName).Select(a => OpenApiPropertyConstructor(a, parameterPrefix, _contextDefinitions)));
            var metadataProperties = string.Join(Environment.NewLine, metadataPropertiesInConfiguration.OrderBy(a => a.ODataPropertyName).Select(a => OpenApiPropertyConstructor(a, metadataPrefix, _metadataDefinitions)));
            openApiText = Regex.Replace(openApiText, "^.*## ParameterPlaceholder.*$", parameterProperties, RegexOptions.Multiline);
            openApiText = Regex.Replace(openApiText, "^.*## MetadataPlaceholder.*$", metadataProperties, RegexOptions.Multiline);

            if (!IsValidYaml(openApiText, out var yamlError))
            {
                LogError($"Yaml parser error after compilation: {yamlError}");
                Environment.Exit(1);
            }

            svgText = svgText.Replace("{organisation}", builder.Configuration.GetValue<string>("Organisation"));

            var appFileContents = new AppFileContents
            {
                CsdlText = csdlText,
                OpenApiText = openApiText,
                SvgText = svgText
            };

            builder.Services.AddSingleton(appFileContents);

            return builder;
        }
    }

    /// <summary>
    /// Creates an OpenAPI property definition from the given OData to SQL property mapping.
    /// </summary>
    /// <param name="propertyMap"></param>
    /// <param name="prefix"></param>
    /// <param name="contextDefinitions"></param>
    /// <returns></returns>
    private static string OpenApiPropertyConstructor(ODataToSqlMap propertyMap, string prefix, Dictionary<string, DigitaleDeltaDefinition> contextDefinitions)
    {
        var split = propertyMap.ODataPropertyName.Split('/');

        if (split.Length < 2)
        {
            return string.Empty;
        }

        var propertyName = split[1];

        if (!contextDefinitions.TryGetValue(propertyName, out var entity))
        {
            return string.Empty;
        }

        var edmTypeString = EdmTypeToOpenApiType(propertyMap.EdmType);
        var descriptionProperty = !string.IsNullOrWhiteSpace(entity.Description)
            ? $"\n          description: {entity.Description}"
            : string.Empty;
        var externalDocs = !string.IsNullOrWhiteSpace(entity.Definition)
            ? $"\n          externalDocs:\n            url: {entity.Definition}\n            description: {entity.System}"
            : string.Empty;
        var template =
$"""
         {propertyMap.ODataPropertyName.Replace(prefix, string.Empty, StringComparison.InvariantCultureIgnoreCase)}:
           type: {edmTypeString}
           nullable: true{descriptionProperty}
           {externalDocs}
 """;

        return template;
    }

    /// <summary>
    /// Converts an EDM type to an OpenAPI type string.
    /// </summary>
    /// <param name="edmType"></param>
    /// <returns></returns>
    private static string EdmTypeToOpenApiType(string edmType)
    {
        return edmType switch
        {
            "Edm.String"         => "string",
            "Edm.Int32"          => "integer\n          format: int32",
            "Edm.Int64"          => "integer\n          format: int64",
            "Edm.Double"         => "number\n          format: double",
            "Edm.Boolean"        => "boolean",
            "Edm.DateTimeOffset" => "string\n          format: date-time",
            _ =>                    "string\n"
        };
    }

    /// <summary>
    /// Gets the property mappings for the specified entity from the configuration.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="entityName"></param>
    /// <returns></returns>
    private static ODataToSqlMap[] GetPropertyMaps(IConfiguration configuration, string entityName) => configuration.GetSection(entityName).Get<ODataToSqlMap[]>() ?? [];

    /// <summary>
    /// Validates that all required property definitions are present in the ObservationConfiguration section.
    /// </summary>
    /// <param name="configuration"></param>
    private static void ValidateRequiredDefinitions(IConfigurationManager configuration)
    {
        var properties = GetPropertyMaps(configuration, $"{observationConfiguration}:{propertyMapping}");

        if (properties.Length == 0)
        {
            LogError($"Fout in configuratie: De sectie '{observationConfiguration}:{propertyMapping}' ontbreekt.");

            return;
        }

        var propertyNames = properties.Select(a => a.ODataPropertyName).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        foreach (var property in _requiredProperties.Where(property => !propertyNames.Contains(property)))
        {
            LogError($"Fout in configuratie: Vereiste definitie '{property}' ontbreekt in {observationConfiguration}:{propertyMapping}.");
        }
    }

    /// <summary>
    /// Validates a configuration section by checking its property mappings, function mappings, and rate limit options.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    private static void ValidateSection(IConfigurationManager configuration, string sectionName)
    {
        ValidatePropertyMapping(configuration, $"{sectionName}:{propertyMapping}");
        ValidateFunctionMapping(configuration, $"{sectionName}:{functionMapping}");
        ValidateRateLimits(configuration, $"{sectionName}:{rateLimitOptions}");
        ValidateIdField(configuration, sectionName);
        ValidateSqlColumnsMatchConfiguration(configuration, sectionName);
    }

    /// <summary>
    /// Validates the token algorithm specified in the configuration.
    /// </summary>
    /// <param name="configuration"></param>
    private static void ValidateTokenAlgorithm(IConfigurationManager configuration)
    {
        var algorithm = configuration.GetValue<string>("TokenAlgorithm");

        if (algorithm != "HMACSHA256" && algorithm != "HMACSHA512")
        {
            LogError("Fout in configuratie: TokenAlgorithm is niet ingesteld. Standaard waarden: HMACSHA256, HMACSHA512.");
        }
    }

    /// <summary>
    /// Validates that a string configuration value meets a minimum length requirement.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="key"></param>
    /// <param name="minLength"></param>
    private static void ValidateStringWithMinimalLength(IConfigurationManager configuration, string key, int minLength)
    {
        var item = configuration.GetValue<string>(key);

        if (item?.Trim().Length < minLength)
        {
            LogError($"Fout in configuratie: {key} moet minimaal {minLength} tekens lang zijn.");
        }
    }

    /// <summary>
    /// Validates the database connection data in the configuration.
    /// </summary>
    /// <param name="configuration"></param>
    private static void ValidateConnectionData(IConfigurationManager configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            LogError("Fout in configuratie: Waarde voor 'DefaultConnection' ontbreekt.");
        }

        var databaseEngine = configuration.GetValue<string>("DatabaseEngine");
        var supportedEngines = new[] { "SqlServer", "Postgres", "MySql", "Oracle", "Sqlite" };

        if (!supportedEngines.Contains(databaseEngine))
        {
            LogError($"Fout in configuratie: DatabaseEngine '{databaseEngine}' is niet ondersteund. Ondersteunde waarden: {string.Join(", ", supportedEngines)}.");
        }

        var connectionFactory = Configure.DatabaseConnectionFactory(configuration);
        var (isValid, error) = ValidateConnection(connectionFactory);

        if (!isValid)
        {
            throw new Exception($"Database connectie geeft een foutmelding: {error}");
        }
    }

    /// <summary>
    /// Validates a database connection using the provided connection factory function.
    /// </summary>
    /// <param name="connectionFactory">
    /// A function that creates a new instance of a <see cref="DbConnection"/> for attempting a connection.
    /// </param>
    /// <returns>
    /// A tuple containing a boolean indicating whether the connection is valid and a string containing the error message if validation fails.
    /// </returns>
    private static (bool isValid, string? error) ValidateConnection(Func<DbConnection> connectionFactory)
    {
        using var connection = connectionFactory();
        try
        {
            connection.Open();

            return connection.State == ConnectionState.Open ? (true, null) : (false, $"Connection state: {connection.State}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Validates the property mappings in the specified configuration section.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    private static void ValidatePropertyMapping(IConfiguration configuration, string sectionName)
    {
        var section = GetPropertyMaps(configuration, sectionName);

        if (section.Length == 0)
        {
            LogError($"Fout in configuratie: De sectie '{sectionName}:{propertyMapping}' ontbreekt.");

            return;
        }

        var oDataMaps = section.ToArray();

        foreach (var item in oDataMaps)
        {
            ValidatePropertyMap(item);
        }

        oDataMaps.GroupBy(a => a.ODataPropertyName)
            .Where(g => g.Count() > 1)
            .ToList()
            .ForEach(g =>
                LogError($"Fout in configuratie: Dubbele functie mapping gevonden in sectie '{g.Key}:{propertyMapping}'."));
    }

    /// <summary>
    /// Validates the function mappings in the specified configuration section.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    private static void ValidateFunctionMapping(IConfiguration configuration, string sectionName)
    {
        var section = configuration.GetSection(sectionName).Get<ODataFunctionMap[]>();

        if (section == null || section.Length == 0)
        {
            LogError($"Fout in configuratie: De sectie '{sectionName}:{functionMapping}' ontbreekt.");

            return;
        }

        var oDataMaps = section.ToArray();

        foreach (var item in oDataMaps)
        {
            ValidateFunctionMap(item);
        }

        oDataMaps.GroupBy(a => a.ODataFunctionName)
            .Where(g => g.Count() > 1)
            .ToList()
            .ForEach(g =>
                LogError($"Fout in configuratie: Dubbele functie mapping gevonden in sectie '{g.Key}:{functionMapping}'."));
    }

    /// <summary>
    /// Validates a single function mapping item.
    /// </summary>
    /// <param name="item"></param>
    private static void ValidateFunctionMap(ODataFunctionMap item)
    {
        if (string.IsNullOrWhiteSpace(item.ODataFunctionName))
        {
            LogError($"Fout in configuratie: ODataFunctionName is vereist in {functionMapping}.");
        }

        if (string.IsNullOrWhiteSpace(item.SqlFunctionFormat))
        {
            LogError($"Fout in configuratie: SqlFunctionFormat is vereist in {functionMapping}.");
        }
        else
        {
            ValidateSqlFragment(item.SqlFunctionFormat, $"{functionMapping}.SqlFunctionFormat ({item.ODataFunctionName})");
        }
    }

    /// <summary>
    /// Validates a single property mapping item.
    /// </summary>
    /// <param name="item"></param>
    private static void ValidatePropertyMap(ODataToSqlMap item)
    {
        if (string.IsNullOrWhiteSpace(item.ODataPropertyName))
        {
            LogError($"Fout in configuratie: ODataPropertyName is vereist in {propertyMapping}.");
        }

        if (string.IsNullOrWhiteSpace(item.Query))
        {
            LogError($"Fout in configuratie: Query is vereist in {propertyMapping}.");
        }
        else
        {
            ValidateSqlFragment(item.Query, $"{propertyMapping}.Query ({item.ODataPropertyName})");
        }

        if (!string.IsNullOrWhiteSpace(item.WhereClausePart))
        {
            ValidateSqlFragment(item.WhereClausePart, $"{propertyMapping}.WhereClausePart ({item.ODataPropertyName})");
        }
    }

    /// <summary>
    /// Validates the rate limit policies in the specified configuration section.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    private static void ValidateRateLimits(IConfiguration configuration, string sectionName)
    {
        var section = configuration.GetSection(sectionName).Get<RateLimitingConfig>();

        section?.Policies.ForEach(a =>
        {
            if (!a.Enabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(a.Name))
            {
                LogError("Fout in configuratie: RateLimitPolicy.Name is vereist.");
            }

            if (string.IsNullOrWhiteSpace(a.PartitionKey))
            {
                LogError("Fout in configuratie: RateLimitPolicy.PartitionKey is vereist.");
            }
        });
    }

    /// <summary>
    /// Validates that the IdField is configured for the specified section.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    private static void ValidateIdField(IConfiguration configuration, string sectionName)
    {
        var idField = configuration.GetValue<string>($"{sectionName}:IdField");

        if (string.IsNullOrWhiteSpace(idField))
        {
            LogError($"Fout in configuratie: IdField is vereist in {sectionName}.");
        }
    }

    /// <summary>
    /// Validates a SQL fragment to prevent SQL injection attacks.
    /// Checks for dangerous SQL patterns that should not appear in configuration.
    /// </summary>
    /// <param name="sqlFragment">The SQL fragment to validate.</param>
    /// <param name="context">Context information for error messages.</param>
    private static void ValidateSqlFragment(string sqlFragment, string context)
    {
        if (string.IsNullOrWhiteSpace(sqlFragment))
        {
            return;
        }

        // Check for common SQL injection patterns that should not appear in configuration
        // These patterns are dangerous in both PostgreSQL and SQL Server
        var dangerousPatterns = new[]
        {
            ";",                // Statement separator - should never be in property/function mappings
            "--",               // SQL comment
            "/*",               // Block comment start
            "*/",               // Block comment end
            "xp_cmdshell",      // SQL Server command shell
            "xp_regread",       // SQL Server registry access
            "xp_regwrite",      // SQL Server registry write
            "sp_executesql",    // SQL Server dynamic SQL
            "openrowset",       // SQL Server/PostgreSQL linked server
            "opendatasource",   // SQL Server external data
            "bulk insert",      // SQL Server bulk operations
            "exec ",            // Execute command (both)
            "execute ",         // Execute command (both)
            "drop ",            // Drop command (both)
            "create ",          // Create command (both)
            "alter ",           // Alter command (both)
            "insert ",          // Insert command (both)
            "update ",          // Update command (both - though UPDATE() function in SQL Server is OK)
            "delete ",          // Delete command (both)
            "truncate ",        // Truncate command (both)
            "grant ",           // Grant command (both)
            "revoke ",          // Revoke command (both)
            "declare ",         // Variable declaration (both)
            "into outfile",     // MySQL file write
            "into dumpfile",    // MySQL file write
            "load_file",        // MySQL file read
            "copy ",            // PostgreSQL COPY command
            "pg_read_file",     // PostgreSQL file read
            "pg_ls_dir",        // PostgreSQL directory listing
        };

        var lowerFragment = sqlFragment.ToLowerInvariant();

        foreach (var pattern in dangerousPatterns)
        {
            if (lowerFragment.Contains(pattern))
            {
                LogError($"Fout in configuratie: {context} bevat verdacht patroon: '{pattern}'");
            }
        }
    }

    /// <summary>
    /// Validates the authentication configuration section.
    /// </summary>
    /// <param name="configuration"></param>
    private static void ValidateAuthenticationConfiguration(IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("AuthenticationConfiguration").Get<AuthenticationSettings>();

        if (authConfig == null)
        {
            LogError("Fout in configuratie: De sectie 'AuthenticationConfiguration' ontbreekt.");
        }

        switch (authConfig?.Type)
        {
            case AuthenticationType.None:
                break;
            case AuthenticationType.OAuth2:
                ArgumentException.ThrowIfNullOrEmpty(authConfig.Authority, "Fout in configuratie: Authority is vereist voor OAuth2 authenticatie.");
                ArgumentException.ThrowIfNullOrEmpty(authConfig.Audience, "Fout in configuratie: Audience is vereist voor OAuth2 authenticatie.");
                break;
            case AuthenticationType.OpenIdConnect:
                ArgumentException.ThrowIfNullOrEmpty(authConfig.Authority, "Fout in configuratie: Authority is vereist voor OpenIdConnect authenticatie.");
                ArgumentException.ThrowIfNullOrEmpty(authConfig.Audience, "Fout in configuratie: Audience is vereist voor OpenIdConnect authenticatie.");
                break;
            case AuthenticationType.MTls:
                break;
            case AuthenticationType.XApiKey:
                ArgumentException.ThrowIfNullOrEmpty(authConfig.ApiKeyHeader, "Fout in configuratie: ApiKeyHeader is vereist voor API Key authenticatie.");
                break;
            default:
                LogError("Ongeldige AuthenticatieType");
                break;
        }
    }

    /// <summary>
    /// Validates the context definitions against the ObservationDefinition property mappings.
    /// </summary>
    /// <param name="configuration">Builder configuration</param>
    /// <param name="validProperties">Known (valid) properties</param>
    /// <param name="prefix">OData prefix name</param>
    /// <param name="propertyMapName">Selected property name</param>
    private static void ValidateDefinitions(IConfiguration configuration, Dictionary<string, DigitaleDeltaDefinition> validProperties, string prefix, string propertyMapName)
    {
        var properties = GetPropertyMaps(configuration, propertyMapName);

        if (properties.Length == 0)
        {
            return;
        }

        var valuesToCheck = properties.Where(a => a.ODataPropertyName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)).ToList();

        foreach (var item in valuesToCheck)
        {
            var contextName = item.ODataPropertyName.Replace(prefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);

            if (!validProperties.ContainsKey(contextName))
            {
                LogError($"Fout in configuratie: ContextDefinition voor '{contextName}' ontbreekt in de opgegeven ContextDefinitions URL.");

                continue;
            }

            if (validProperties[contextName].ODataDataType != item.EdmType)
            {
                LogError($"Fout in configuratie: ContextDefinition type mismatch voor '{contextName}'. Verwacht: '{item.EdmType}', Gevonden: '{validProperties[contextName]}'.");
            }

            // Apply DisallowInFilter from definition to property map
            if (validProperties[contextName].DisallowInFilter == true)
            {
                item.DisallowInFilter = true;
            }
        }
    }

    /// <summary>
    /// Retrieves context or metadata definitions from a CSV file located at the specified URL.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="typeName"></param>
    /// <returns></returns>
    private static Dictionary<string, DigitaleDeltaDefinition> GetDefinitions(string? url, string typeName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            LogError($"Fout in configuratie: {typeName} URL is vereist.");

            return new Dictionary<string, DigitaleDeltaDefinition>(StringComparer.InvariantCultureIgnoreCase);
        }

        string data;

        try
        {
            using var client = new HttpClient();
            data = client.GetStringAsync(url).GetAwaiter().GetResult();
        }
        catch(Exception e)
        {
            if (Debugger.IsAttached)
            {
                try
                {
                    data = File.ReadAllText(url);
                }
                catch (Exception ex)
                {
                    LogError($"Fout bij ophalen/parsen van {url}: {ex.Message}");

                    return new Dictionary<string, DigitaleDeltaDefinition>(StringComparer.InvariantCultureIgnoreCase);
                }
            }
            else
            {
                LogError($"Fout bij ophalen/parsen van url: {url}: {e.Message}");

                return new Dictionary<string, DigitaleDeltaDefinition>(StringComparer.InvariantCultureIgnoreCase);
            }

        }

        var csvConfig = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim
            };

        using var reader = new CsvHelper.CsvReader(new StringReader(data), csvConfig);
        var dictionary = new Dictionary<string, DigitaleDeltaDefinition>(StringComparer.InvariantCultureIgnoreCase);

        reader.Read();
        reader.ReadHeader();

        var nameIndex = reader.HeaderRecord.IndexOf("Naam");
        var typeIndex = reader.HeaderRecord.IndexOf("OData data type");
        var descriptionIndex = reader.HeaderRecord.IndexOf("Omschrijving");
        var definitionIndex = reader.HeaderRecord.IndexOf("Definitie");
        var systemIndex = reader.HeaderRecord.IndexOf("Systeem");
        var disallowInFilterIndex = reader.HeaderRecord.IndexOf("DisallowInFilter");
        while (reader.Read())
        {
            var name = reader.GetField<string>(nameIndex)?.Trim();
            var type = reader.GetField<string>(typeIndex)?.Trim();
            var description = reader.GetField<string>(descriptionIndex)?.Trim();
            var definition = definitionIndex == -1 ? string.Empty : reader.GetField<string>(definitionIndex);
            var system = systemIndex == -1 ? string.Empty : reader.GetField<string>(systemIndex);
            var disallowInFilter = disallowInFilterIndex == -1 ? null : reader.GetField<bool?>(disallowInFilterIndex);

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            var definitionRecord = new DigitaleDeltaDefinition { Name = name, ODataDataType = type, Description = description, Definition = definition, System = system, DisallowInFilter = disallowInFilter };

            if (!dictionary.TryAdd(name, definitionRecord))
            {
                LogError($"Fout in configuratie: Dubbele gevonden voor '{name}' in {url}.");
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Checks if a file exists and logs an error if it does not.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="fileName"></param>
    private static void ValidateFileExists(string context, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            LogError($"Fout in configuratie: Bestandsnaam voor {context} ontbreekt.");

            return;
        }

        if (!File.Exists(fileName))
        {
            LogError($"Fout in configuratie: Bestand '{fileName}' ontbreekt.");
        }
    }

    /// <summary>
    /// Logs an error message and updates the validation status.
    /// </summary>
    /// <param name="error"></param>
    private static void LogError(string error)
    {
        _isValid = false;
        Log.Error("Configuration error: {error}", error);
        Console.Error.WriteLine($"Configuration error: {error}"); // Always log to the console.
    }

    /// <summary>
    /// Validates that all columns referenced in the configuration PropertyMapping exist in the SQL query.
    /// </summary>
    /// <param name="configuration">The configuration manager.</param>
    /// <param name="sectionName">The section name (e.g., "ObservationConfiguration").</param>
    private static void ValidateSqlColumnsMatchConfiguration(IConfigurationManager configuration, string sectionName)
    {
        var dataQueryFile = configuration.GetValue<string>($"{sectionName}:DataQueryFile");

        if (string.IsNullOrWhiteSpace(dataQueryFile) || !File.Exists(dataQueryFile))
        {
            return; // Already validated by ValidateFileExists
        }

        var sqlQuery = File.ReadAllText(dataQueryFile);
        var propertyMaps = GetPropertyMaps(configuration, $"{sectionName}:{propertyMapping}");

        if (propertyMaps.Length == 0)
        {
            return; // Already validated by ValidatePropertyMapping
        }

        var sqlColumns = ExtractColumnAliases(sqlQuery);

        foreach (var map in propertyMaps)
        {
            var columnName = map.ColumnName.Trim();

            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            if (!sqlColumns.Contains(columnName))
            {
                LogError($"Fout in configuratie: Kolom '{columnName}' uit {sectionName}:{propertyMapping} (ODataPropertyName: '{map.ODataPropertyName}') komt niet voor in SQL query '{dataQueryFile}'.");
            }
        }
    }

    /// <summary>
    /// Extracts column aliases from a SQL SELECT statement.
    /// Parses the text between SELECT and FROM to find all column aliases.
    /// </summary>
    /// <param name="sql">The SQL query text.</param>
    /// <returns>A set of column aliases found in the query.</returns>
    private static HashSet<string> ExtractColumnAliases(string sql)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract the SELECT clause (between SELECT and FROM)
        var selectMatch = Regex.Match(sql, @"\bselect\s+(.*?)\s+from\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!selectMatch.Success)
        {
            return aliases;
        }

        var selectClause = selectMatch.Groups[1].Value;

        // Split by comma, but respect nested parentheses and strings
        var columns = SplitSelectColumns(selectClause);

        foreach (var column in columns)
        {
            var trimmed = column.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Check if there's an AS clause (case insensitive)
            var asMatch = Regex.Match(trimmed, @"\s+as\s+(\w+)\s*$", RegexOptions.IgnoreCase);

            if (asMatch.Success)
            {
                // Has explicit AS alias
                aliases.Add(asMatch.Groups[1].Value);
            }
            else
            {
                // There is no AS clause used - the column name itself is the 'alias'.
                // Extract the last word (handling cases like "table.column" -> "column")
                var lastWordMatch = Regex.Match(trimmed, @"[\w]+\s*$");

                if (lastWordMatch.Success)
                {
                    aliases.Add(lastWordMatch.Value.Trim());
                }
            }
        }

        return aliases;
    }

    /// <summary>
    /// Splits a SQL SELECT clause by commas while respecting parentheses and string literals.
    /// </summary>
    /// <param name="selectClause">The SELECT clause text.</param>
    /// <returns>A list of individual column expressions.</returns>
    private static List<string> SplitSelectColumns(string selectClause)
    {
        var columns = new List<string>();
        var current = new System.Text.StringBuilder();
        var depth = 0;
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < selectClause.Length; i++)
        {
            var c = selectClause[i];

            // Handle string literals
            if ((c == '\'' || c == '"') && (i == 0 || selectClause[i - 1] != '\\'))
            {
                if (!inString)
                {
                    inString = true;
                    stringChar = c;
                }
                else if (c == stringChar)
                {
                    inString = false;
                }
                current.Append(c);
                continue;
            }

            if (inString)
            {
                current.Append(c);
                continue;
            }

            // Track parentheses depth
            if (c == '(')
            {
                depth++;
                current.Append(c);
            }
            else if (c == ')')
            {
                depth--;
                current.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                // Top-level comma - this separates columns
                columns.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        // Add the last column
        if (current.Length > 0)
        {
            columns.Add(current.ToString());
        }

        return columns;
    }

    /// <summary>
    /// Validates if the provided YAML text is correctly formatted according to YAML syntax.
    /// </summary>
    /// <param name="yamlText">The input string containing the YAML text to validate.</param>
    /// <param name="error">Returns the validation error message if the YAML text is invalid; otherwise, null.</param>
    /// <returns>True if the YAML text is valid; otherwise, false.</returns>
    /// <exception cref="YamlException">Thrown if an error occurs while parsing the YAML text.</exception>
    public static bool IsValidYaml(string yamlText, out string? error)
    {
        try
        {
            var yaml = new YamlStream();
            using var reader = new StringReader(yamlText);

            yaml.Load(reader);
            error = null;

            return true;
        }
        catch (YamlException ex)
        {
            error = ex.Message;

            return false;
        }
    }
}
