// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Dapper;
using DigitaleDelta.ErrorHandling;
using DigitaleDelta.ODataTranslator;
using DigitaleDelta.ODataWriter;
using DigitaleDelta.RequestHelpers;
using DigitaleDelta.SkipToken;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DigitaleDelta.QueryService;

/// <summary>
/// Generic query service.
/// </summary>
public class QueryService
{
    private readonly ODataQueryServiceParameters _parameters;
    private readonly ILogger _logger;
    private readonly IMemoryCache _cache;
    private readonly IRequestLogger _requestLogger;
    private readonly IAuthorization _authorizationService;

    /// <summary>
    /// Generic query service.
    /// </summary>
    /// <param name="parameters">Query Service Parameters</param>
    /// <param name="logger">Logger</param>
    /// <param name="cache">Cache service (for count queries)</param>
    /// <param name="requestLogger">Request logger service</param>
    /// <param name="authorizationService">Authorisation service</param>
    public QueryService(ODataQueryServiceParameters parameters, ILogger logger, IMemoryCache cache, IRequestLogger requestLogger, IAuthorization authorizationService)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(requestLogger);
        ArgumentNullException.ThrowIfNull(authorizationService);

        _parameters = parameters;
        _logger = logger;
        _cache = cache;
        _requestLogger = requestLogger;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Processes an HTTP request and retrieves data based on OData query options with support for filtering, pagination, and access control.
    /// </summary>
    /// <param name="httpContext">The current HTTP context associated with the request.</param>
    /// <param name="oDataQueryOptions">The OData query options specifying filters, pagination, and other query parameters.</param>
    /// <param name="parameterPrefix">Prefix used for parameters. Database engine dependent.</param>
    /// <returns>
    /// A tuple containing:
    /// 1. An ODataValidationException if the query is invalid, otherwise null.
    /// 2. The query result data as a collection of dictionaries, or null in case of an exception.
    /// 3. The skip token for pagination, or null if not applicable.
    /// 4. The total count of matching records, or null if not applicable.
    /// 5. A boolean indicating whether more data exists beyond the current page.
    /// </returns>
    /// <exception cref="ODataValidationException">Thrown if the OData query contains validation errors.</exception>
    public async Task<JsonResult?> ProcessRequestAsync(HttpContext httpContext, ODataQueryOptions oDataQueryOptions, char parameterPrefix)
    {
        var start     = DateTime.UtcNow;

        _requestLogger.LogRequest(httpContext.Request.GetUrl(), oDataQueryOptions.ClaimsPrincipal, httpContext.TraceIdentifier, start);

        var convertor = new ODataToSqlConverter(_parameters.PropertyMap, _parameters.FunctionMap, srid: oDataQueryOptions.CrsId ?? 4258, parameterPrefix: parameterPrefix);

        if (!convertor.TryConvert(oDataQueryOptions.Filter, out var error, out var whereClause))
        {
            throw new ODataValidationException(error ?? string.Empty);
        }

        var authorizationResult = await _authorizationService.TryAuthorizeAsync(oDataQueryOptions.ClaimsPrincipal).ConfigureAwait(false);

        if (!authorizationResult.authorised)
        {
            throw new ODataValidationException("Not authorized to access this resource.");
        }

        var queryResult = await GetDataAsync(oDataQueryOptions, whereClause, authorizationResult.access, httpContext.TraceIdentifier).ConfigureAwait(false);

        if (queryResult == null || queryResult.HasError)
        {
            throw new ODataValidationException($"Error: {error}");
        }

        _requestLogger.LogResponse(httpContext.TraceIdentifier, succeeded: true, DateTimeOffset.Now, DateTime.UtcNow - start, queryResult.Data?.Count());

        return CreateResponse(httpContext, queryResult, oDataQueryOptions);
    }

    /// <summary>
    /// Creates an HTTP response containing the OData result set with pagination, metadata, and additional query options.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="queryResult"></param>
    /// <param name="oDataQueryOptions">The OData query options applied to the request.</param>
    /// <returns>An OkObjectResult containing the constructed OData response.</returns>
    private JsonResult CreateResponse(HttpContext context, QueryResult queryResult, ODataQueryOptions oDataQueryOptions)
    {
        context.Request.SetPreferenceAppliedResponseHeader();

        // Filter out properties marked as ExcludeFromResponse
        var excludedProperties = _parameters.PropertyMap
            .Where(kvp => kvp.Value.ExcludeFromResponse)
            .Select(kvp => kvp.Value.ODataPropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filteredData = (queryResult.Data ?? [])
            .Select(entity => entity
                .Where(kvp => !excludedProperties.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            .ToList();

        var response = filteredData.CreateODataResponse()
            .WithBaseUrl(context.Request.GetBaseUrl())
            .IncludeCount(oDataQueryOptions.Count ?? false)
            .WithEntitySet(_parameters.EntitySetName)
            .WithSelectProperties(oDataQueryOptions.Select?.AsEnumerable())
            .WithPagination(queryResult.SkipToken, queryResult.TotalCount)
            .HasMoreData(queryResult.MoreData)
            .Build();

        return new JsonResult(response);
    }

    /// <summary>
    /// Asynchronously retrieves data from the reference table.
    /// </summary>
    /// <param name="queryParameters">ODataQueryParameters.</param>
    /// <param name="whereClause"></param>
    /// <param name="access"></param>
    /// <param name="requestId">Request identifier for tracing</param>
    /// <returns></returns>
    /// <exception cref="ODataValidationException">Thrown if an error occurs during the query execution.</exception>
    private async Task<QueryResult?> GetDataAsync(ODataQueryOptions queryParameters, ODataToSqlConverter.SqlResult? whereClause, string access, string requestId)
    {
        var skipTokenHelper = new SkipTokenHelper(_parameters.TokenSecret, _parameters.HmacAlgorithm);

        if (!skipTokenHelper.TryExtractFromUrl(queryParameters.Url, out var skipToken, out var error))
        {
            var e = new ODataValidationException($"ValidationError: {error}");

            _logger.LogError(e, "Error in GetDataAsync");

            return new QueryResult { HasError = true, ErrorDetails = e.Message };
        }

        // Get the EdmType of the IdField for proper type casting in skiptoken queries
        var idFieldEdmType = _parameters.ReversePropertyMap.TryGetValue(_parameters.IdField, out var idMap)
            ? idMap.EdmType
            : "Edm.String";

        var (dataSqlStatement, dataParams) = ConstructDataQuery(whereClause, _parameters.DataQuery, skipToken, queryParameters, access, _parameters.IdField, idFieldEdmType);
        var (countSqlStatement, countParams) = ConstructCountQuery(whereClause, _parameters.CountQuery, queryParameters, access);
        int? totalCount = null;
        await using var connection = _parameters.ConnectionFactory();

        if (queryParameters.Count == true)
        {
            var countCacheKey = $"{queryParameters.Filter?.GetText() ?? string.Empty}/{GetUniversalName(queryParameters.ClaimsPrincipal)}/count";

            totalCount = await _cache.GetOrCreateAsync(countCacheKey, cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_parameters.CountCachePeriod);

                return connection.QuerySingleAsync<int>(countSqlStatement, countParams, commandTimeout: 300);
            });
        }

        if (totalCount == 0 && queryParameters.Count == true)
        {
            return null;
        }

        try
        {
            _logger.LogInformation("RequestId: {RequestId} - Executing SQL query: {SqlQuery}", requestId, dataSqlStatement);
            await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(dataSqlStatement, dataParams, commandTimeout: 120)).ConfigureAwait(false);
            var orderedPropertyMaps = DbRowMaterializer.DbRowMaterializer.CreateReverseOrderedMap(reader, _parameters).ToDictionary(a => a.ODataPropertyName);
            var data = await DbRowMaterializer.DbRowMaterializer.MaterializeToListAsync(reader, orderedPropertyMaps, queryParameters.OmitNulls, queryParameters.Top + 1, _logger, queryParameters.CrsId).ConfigureAwait(false);
            var hasMore = data.Count > queryParameters.Top;
            var dataToReturn = hasMore ? data.Take(queryParameters.Top).ToList() : data;
            var idODataKey = _parameters.ReversePropertyMap.TryGetValue(_parameters.IdField, out var idKey) ? idKey.ODataPropertyName : "Id";
            var lastIdForPage = dataToReturn.LastOrDefault()?.TryGetValue(idODataKey, out var idVal) == true ? idVal?.ToString() : null;

            skipTokenHelper.TryConstructFromUrl(queryParameters.Url ?? string.Empty, lastIdForPage, out var newSkipToken);

            return new QueryResult { Data = dataToReturn, SkipToken = newSkipToken, TotalCount = totalCount, MoreData = hasMore };
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "RequestId: {RequestId} - Database query failed: {Message}. SQL Query: {SqlQuery}", requestId, ex.Message, dataSqlStatement);

            // Return generic error to user, don't expose database details
            return new QueryResult { HasError = true, ErrorDetails = "Database error occurred while processing the request." };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RequestId: {RequestId} - Query cancelled by client. SQL Query: {SqlQuery}", requestId, dataSqlStatement);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RequestId: {RequestId} - Unexpected error during query execution. SQL Query: {SqlQuery}", requestId, dataSqlStatement);

            // Return generic error to user, don't expose internal details
            return new QueryResult { HasError = true, ErrorDetails = "An unexpected error occurred while processing the request." };
        }
    }

    /// <summary>
    /// Constructs an SQL query by injecting the provided parameters, including filters, pagination tokens, and access control information.
    /// </summary>
    /// <param name="whereClause">The resulting SQL WHERE clause from the OData query parsing process.</param>
    /// <param name="query">The base SQL query string with placeholders for dynamic parameters.</param>
    /// <param name="skipToken">The skip token containing pagination information such as the last served ID.</param>
    /// <param name="queryOptions">The OData query options object containing details like top and CRS ID.</param>
    /// <param name="access">The access control parameter used for filtering based on access constraints.</param>
    /// <param name="idField">The name of the ID field used for pagination.</param>
    /// <param name="idFieldEdmType">The EDM type of the ID field (e.g., "Edm.Int32", "Edm.String", "Edm.Guid").</param>
    /// <returns>
    /// A formatted SQL query string ready for execution, incorporating all provided parameters such as filters, pagination, and access control directives.
    /// </returns>
    private static (string Sql, DynamicParameters Params) ConstructDataQuery(
        ODataToSqlConverter.SqlResult? whereClause,
        string query,
        SkipToken.SkipToken? skipToken,
        ODataQueryOptions queryOptions,
        string access,
        string idField,
        string idFieldEdmType)
    {
        // Validate idField to prevent SQL injection
        if (!IsValidSqlIdentifier(idField))
        {
            throw new ArgumentException($"Invalid SQL identifier: {idField}", nameof(idField));
        }

        // Validate access clause to prevent SQL injection
        ValidateAccessClause(access);

        var dp = new DynamicParameters();

        // merge existing where parameters (if any)
        if (whereClause?.Parameters != null)
        {
            dp.AddDynamicParams(whereClause.Parameters);
        }

        // Add strongly typed parameters instead of injecting values into SQL text
        dp.Add("@limit", queryOptions.Top + 1);
        dp.Add("@srid", queryOptions.CrsId);

        var whereStatement = whereClause?.Sql ?? "1 = 1";
        string skipTokenCondition;

        if (skipToken == null)
        {
            skipTokenCondition = " 1 = 1 ";
        }
        else
        {
            // Determine the appropriate parameter value based on EDM type
            // For numeric types, parse the string to the appropriate type
            // For string/GUID types, keep as string
            object lastIdValue = idFieldEdmType switch
            {
                "Edm.Int32" => int.TryParse(skipToken.LastId, out var i32) ? i32 : throw new InvalidOperationException($"Cannot parse LastId '{skipToken.LastId}' as Int32"),
                "Edm.Int64" => long.TryParse(skipToken.LastId, out var i64) ? i64 : throw new InvalidOperationException($"Cannot parse LastId '{skipToken.LastId}' as Int64"),
                "Edm.Guid" => Guid.TryParse(skipToken.LastId, out var guid) ? guid : throw new InvalidOperationException($"Cannot parse LastId '{skipToken.LastId}' as Guid"),
                "Edm.String" => skipToken.LastId,
                _ => skipToken.LastId  // Default to string for unknown types
            };

            dp.Add("@lastId", lastIdValue);
            skipTokenCondition = $"{idField} > @lastId";
        }

        var sqlStatement = query
            .Replace("@where", whereStatement)
            .Replace("@skiptoken", skipTokenCondition)
            .Replace("@access", access);

        return (sqlStatement, dp);
    }

    /// <summary>
    /// Constructs an SQL query by injecting the provided parameters, including filters, pagination tokens, and access control information.
    /// </summary>
    /// <param name="whereClause">The resulting SQL WHERE clause from the OData query parsing process.</param>
    /// <param name="query">The base SQL query string with placeholders for dynamic parameters.</param>
    /// <param name="queryOptions">The OData query options object containing details like top and CRS ID.</param>
    /// <param name="access">The access control parameter used for filtering based on access constraints.</param>
    /// <returns>
    /// A formatted SQL query string ready for execution, incorporating all provided parameters such as filters, pagination, and access control directives.
    /// </returns>
    private static (string Sql, DynamicParameters Params) ConstructCountQuery(
        ODataToSqlConverter.SqlResult? whereClause,
        string query,
        ODataQueryOptions queryOptions,
        string access)
    {
        // Validate access clause to prevent SQL injection
        ValidateAccessClause(access);

        var dp = new DynamicParameters();

        // merge existing where parameters (if any)
        if (whereClause?.Parameters != null)
        {
            dp.AddDynamicParams(whereClause.Parameters);
        }

        // Add strongly typed parameters instead of injecting values into SQL text
        dp.Add("@access", access);
        dp.Add("@srid", queryOptions.CrsId);

        var whereStatement = whereClause?.Sql ?? "1 = 1";
        var sqlStatement = query
            .Replace("@where", whereStatement)
            .Replace("@skiptoken", "1 = 1")  // Count queries should not use skiptoken
            .Replace("@access", access);

        return (sqlStatement, dp);
    }

    /// <summary>
    /// Validates that a string is a valid SQL identifier (table/column name).
    /// Prevents SQL injection by ensuring only alphanumeric characters and underscores are allowed.
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <returns>True if the identifier is valid, false otherwise.</returns>
    private static bool IsValidSqlIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        // Allow letters, digits, underscores, and dots (for schema.table notation)
        // Must start with letter or underscore
        return Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_\.]*$");
    }

    /// <summary>
    /// Validates the access clause to prevent SQL injection attacks.
    /// Checks for common SQL injection patterns.
    /// </summary>
    /// <param name="access">The access clause to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the access clause contains suspicious patterns.</exception>
    private static void ValidateAccessClause(string access)
    {
        if (string.IsNullOrWhiteSpace(access))
        {
            throw new ArgumentException("Access clause cannot be null or empty.", nameof(access));
        }

        // Check for common SQL injection patterns
        var dangerousPatterns = new[]
        {
            ";",           // Statement separator
            "--",          // SQL comment
            "/*",          // Block comment start
            "*/",          // Block comment end
            "xp_",         // Extended stored procedures
            "sp_",         // System stored procedures (be careful, might be legitimate)
            "exec ",       // Execute command
            "execute ",    // Execute command
            "drop ",       // Drop command
            "create ",     // Create command
            "alter ",      // Alter command
            "insert ",     // Insert command
            "update ",     // Update command (in access clause context)
            "delete ",     // Delete command
            "truncate ",   // Truncate command
            "grant ",      // Grant command
            "revoke ",     // Revoke command
            "union ",      // Union injection
            "declare ",    // Variable declaration
            "char(",       // Character encoding tricks
            "nchar(",      // Character encoding tricks
            "cast(",       // Type casting tricks
            "convert("     // Type conversion tricks
        };

        var lowerAccess = access.ToLowerInvariant();

        foreach (var pattern in dangerousPatterns)
        {
            if (lowerAccess.Contains(pattern))
            {
                throw new ArgumentException($"Access clause contains suspicious pattern: '{pattern}'", nameof(access));
            }
        }
    }

    /// <summary>
    /// Retrieves a universal identifier or name from a provided ClaimsPrincipal, prioritizing certain identifiers or names in a predefined order.
    /// </summary>
    /// <param name="principal">
    /// The ClaimsPrincipal from which to extract the universal name or identifier.
    /// </param>
    /// <returns>
    /// A string representing the universal name or identifier if found, or null if no suitable value is available.
    /// </returns>
    private static string? GetUniversalName(ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return null;
        }

        var id =
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("client_id")?.Value ??
            principal.FindFirst("x500.distinguished_name")?.Value ??
            principal.FindFirst("subject")?.Value ??
            principal.FindFirst("X-API-KEY")?.Value;

        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        var name =
            principal.FindFirst("preferred_username")?.Value ??
            principal.FindFirst(ClaimTypes.Name)?.Value ??
            principal.Identity?.Name;

        return !string.IsNullOrWhiteSpace(name) ? name : null;
    }
}
