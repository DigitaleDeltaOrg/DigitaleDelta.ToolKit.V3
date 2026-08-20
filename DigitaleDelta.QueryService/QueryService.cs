// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Dapper;
using DigitaleDelta.CoverageJSON.Models;
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
public partial class QueryService
{
    private readonly ODataQueryServiceParameters _parameters;
    private readonly ILogger _logger;
    private readonly IMemoryCache _cache;
    private readonly IRequestLogger _requestLogger;
    private readonly IAuthorisation _authorisationService;
    private readonly HashSet<string> _excludedProperties;
    private readonly CoverageConfiguration? _coverageConfiguration;

    /// <summary>
    /// Generic query service.
    /// </summary>
    /// <param name="parameters">Query Service Parameters</param>
    /// <param name="logger">Logger</param>
    /// <param name="cache">Cache service (for count queries)</param>
    /// <param name="requestLogger">Request logger service</param>
    /// <param name="authorisationService">Authorisation service</param>
    /// <param name="coverageConfiguration"></param>
    public QueryService(ODataQueryServiceParameters parameters, ILogger logger, IMemoryCache cache, IRequestLogger requestLogger, IAuthorisation authorisationService, CoverageConfiguration? coverageConfiguration)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(requestLogger);
        ArgumentNullException.ThrowIfNull(authorisationService);

        _parameters = parameters;
        _logger = logger;
        _cache = cache;
        _requestLogger = requestLogger;
        _authorisationService = authorisationService;
        _excludedProperties = _parameters.PropertyMap
            .Where(kvp => kvp.Value.ExcludeFromResponse)
            .Select(kvp => kvp.Value.ODataPropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _coverageConfiguration = coverageConfiguration;
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

        var authorizationResult = await _authorisationService.TryAuthorizeAsync(oDataQueryOptions.ClaimsPrincipal).ConfigureAwait(false);

        if (!authorizationResult.authorised)
        {
            throw new ODataApiException("Not authorized to access this resource.", (int)HttpStatusCode.Forbidden);
        }

        var queryResult = await GetDataAsync(oDataQueryOptions, whereClause, authorizationResult.access, httpContext.TraceIdentifier).ConfigureAwait(false);

        if (queryResult?.HasError == true)
        {
            throw new ODataValidationException(queryResult.ErrorDetails ?? "ValidationError: An error occurred while processing the request.");
        }

        if (!authorizationResult.authorised)
        {
            throw new ODataApiException("Not authorized to access this resource.", StatusCodes.Status403Forbidden, "Forbidden");
        }

        _requestLogger.LogResponse(httpContext.TraceIdentifier, succeeded: true, DateTimeOffset.Now, DateTime.UtcNow - start, queryResult?.Data?.Count);

        return CreateResponse(httpContext, queryResult ?? new QueryResult(), oDataQueryOptions);
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

        var data = queryResult.Data ?? [];

        RemoveExcludedProperties(data);

        var response = data.CreateODataResponse()
            .WithBaseUrl(context.Request.GetBaseUrl())
            .IncludeCount(oDataQueryOptions.Count ?? false)
            .WithEntitySet(_parameters.EntitySetName)
            .WithSelectProperties(oDataQueryOptions.Select?.AsEnumerable())
            .WithQuery(context.Request.QueryString.Value)
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

        // Get the EdmType of the 'IdField' for proper casting of type in skiptoken queries
        var idFieldEdmType = _parameters.ReversePropertyMap.TryGetValue(_parameters.IdField, out var idMap)
            ? idMap.EdmType
            : "Edm.String";

        var (dataSqlStatement, dataParams) = ConstructDataQuery(whereClause, _parameters.DataQuery, skipToken, queryParameters, access, _parameters.IdField, idFieldEdmType);
        var (countSqlStatement, countParams) = ConstructCountQuery(whereClause, _parameters.CountQuery, queryParameters, access);
        int? totalCount = null;
        await using var connection = _parameters.ConnectionFactory();

        if (queryParameters.Count == true)
        {
            var countCacheKey = $"{_parameters.EntitySetName}/{queryParameters.Filter?.GetText() ?? string.Empty}/{GetUniversalName(queryParameters.ClaimsPrincipal)}/count";

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
            var paramDump = string.Join(", ", dataParams.ParameterNames.Select(n => $"{n}={dataParams.Get<object?>(n) ?? "NULL"}"));
            _logger.LogInformation("RequestId: {RequestId} - Executing SQL query: {SqlQuery} -- Params: {Params}", requestId, dataSqlStatement, paramDump);

            await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(dataSqlStatement, dataParams, commandTimeout: 120)).ConfigureAwait(false);
            var idODataKey = _parameters.ReversePropertyMap.TryGetValue(_parameters.IdField, out var idKey) ? idKey.ODataPropertyName : "Id";
            var materializerExcludedProperties = GetMaterializerExcludedProperties(idODataKey);
            var orderedPropertyMaps = DbRowMaterializer.DbRowMaterializer.CreateReverseOrderedMap(reader, _parameters).ToDictionary(a => a.ODataPropertyName);
            var data = await DbRowMaterializer.DbRowMaterializer.MaterializeToListAsync(reader, orderedPropertyMaps, queryParameters.OmitNulls, queryParameters.Top + 1, _logger, queryParameters.CrsId, excludeProperties: materializerExcludedProperties).ConfigureAwait(false);
            var hasMore = data.Count > queryParameters.Top;

            if (hasMore)
            {
                data.RemoveAt(data.Count - 1);
            }

            if (_coverageConfiguration is { Transformer: not null })
            {
                foreach (var row in data)
                {
                    if (!row.TryGetValue("Result/Coverage", out var coverageData) || coverageData == null)
                    {
                        continue;
                    }

                    var convertedData =  _coverageConfiguration.Transformer.Transform(JsonElement.Parse(coverageData.ToString() ?? string.Empty));
                    if (convertedData is null)
                    {
                        continue;
                    }

                    Shape(convertedData, _coverageConfiguration);
                    Slice(convertedData, _coverageConfiguration);
                    Aggregate(convertedData, _coverageConfiguration);

                    row["Result/Coverage"] = convertedData;
                }
            }

            var lastIdForPage = data.LastOrDefault()?.TryGetValue(idODataKey, out var idVal) == true ? idVal?.ToString() : null;

            skipTokenHelper.TryConstructFromUrl(queryParameters.Url ?? string.Empty, lastIdForPage, out var newSkipToken);

            return new QueryResult { Data = data, SkipToken = newSkipToken, TotalCount = totalCount, MoreData = hasMore };
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
    /// Shapes the coverage data by filtering axes and ranges based on the provided coverage configuration.
    /// </summary>
    /// <param name="coverage">The coverage object containing domain and range data to be shaped.</param>
    /// <param name="coverageConfiguration">The configuration specifying which axes and ranges should be retained in the coverage data.</param>
    private static void Shape(Coverage coverage, CoverageConfiguration coverageConfiguration)
    {
        if (coverageConfiguration.SelectRanges is null && coverageConfiguration.SelectAxes is null)
        {
            return;
        }

        var selectedRangeNames = coverageConfiguration.SelectRanges?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedAxisNames = coverageConfiguration.SelectRanges?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (coverageConfiguration.SelectRanges is not null && selectedRangeNames is not null)
        {

            var rangesToRemove = coverage.Ranges.Keys.Where(name => !selectedRangeNames.Contains(name)).ToList();

            foreach (var rangeName in rangesToRemove)
            {
                coverage.Ranges.Remove(rangeName);
            }

            if (coverageConfiguration.SelectAxes is null)
            {
                return;
            }

            foreach (var range in coverage.Ranges.Values)
            {
                if (range.AxisNames is null)
                {
                    continue;
                }

                var invalidAxes = range.AxisNames.Where(axis => !(selectedAxisNames ?? []).Contains(axis)).ToList();

                if (invalidAxes.Count > 0)
                {
                    throw new InvalidOperationException($"Range '{range.Name}' gebruikt niet-geselecteerde assen: {string.Join(", ", invalidAxes)}.");
                }
            }
        }

        if (selectedAxisNames is null)
        {
            return;
        }

        foreach (var axisName in coverage.Domain.Axes.Keys.ToList())
        {
            if (!selectedAxisNames.Contains(axisName))
            {
                coverage.Domain.Axes.Remove(axisName);
            }
        }
    }

    /// <summary>
    /// Aggregates the coverage data based on the specified configuration parameters.
    /// </summary>
    /// <param name="coverage">The coverage data to be aggregated.</param>
    /// <param name="coverageConfiguration">The configuration settings that determine the aggregation behavior, including the function and duration.</param>
    /// <exception cref="InvalidOperationException">Thrown when the aggregation duration is invalid, unsupported ISO 8601 durations are provided, or the duration is not greater than zero.</exception>
  private static void Aggregate(Coverage coverage, CoverageConfiguration coverageConfiguration)
{
    if (string.IsNullOrWhiteSpace(coverageConfiguration.AggregateFunction) || string.IsNullOrWhiteSpace(coverageConfiguration.AggregateDuration))
    {
        return;
    }

    if (!coverage.Domain.Axes.TryGetValue("t", out var timeAxis) || timeAxis.Values is null || timeAxis.Values.Count == 0)
    {
        return;
    }

    var period = ParseAggregationPeriod(coverageConfiguration.AggregateDuration);
    var times = new DateTimeOffset[timeAxis.Values.Count];

    for (var i = 0; i < times.Length; i++)
    {
        times[i] = timeAxis.Values[i].GetDateTimeOffset();
    }

    var bucketForTime = new long[times.Length];
    var bucketKeys = new SortedSet<long>();

    for (var i = 0; i < times.Length; i++)
    {
        var bucket = period.CalendarMonths is not null
            ? GetCalendarBucket(times[i], period.CalendarMonths.Value)
            : GetFixedBucket(times, times[i], period.FixedDuration!.Value);

        bucketForTime[i] = bucket;
        bucketKeys.Add(bucket);
    }

    var orderedBucketKeys = bucketKeys.ToArray();
    var bucketIndexes = new Dictionary<long, int>(orderedBucketKeys.Length);

    for (var i = 0; i < orderedBucketKeys.Length; i++)
    {
        bucketIndexes[orderedBucketKeys[i]] = i;
    }

    var bucketStarts = new List<JsonElement>(orderedBucketKeys.Length);

    foreach (var bucket in orderedBucketKeys)
    {
        var bucketStart = period.CalendarMonths is not null
            ? GetCalendarBucketStart(bucket, period.CalendarMonths.Value)
            : GetFixedBucketStart(times, bucket, period.FixedDuration!.Value);

        bucketStarts.Add(JsonSerializer.SerializeToElement(bucketStart));
    }

    coverage.Domain.Axes["t"] = timeAxis with
    {
        Values = bucketStarts
    };

    foreach (var range in coverage.Ranges.Values)
    {
        AggregateRange(range, bucketForTime, bucketIndexes, bucketStarts.Count, coverageConfiguration.AggregateFunction);
    }
}

    /// <summary>
    /// Aggregates the values of a range over a specified time dimension using a defined aggregation function.
    /// </summary>
    /// <param name="range">The range of data to be aggregated, including its values, axis names, and shape.</param>
    /// <param name="bucketForTime">An array that maps each time index to its corresponding bucket index.</param>
    /// <param name="bucketIndexes">A dictionary mapping bucket keys to their respective indexes.</param>
    /// <param name="bucketCount">The total number of buckets used for aggregation.</param>
    /// <param name="aggregateFunction">The name of the aggregation function to apply (e.g., "count", "sum", "mean", "min", "max").</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the range's shape is invalid for the time axis, or when the range's values count does not match its shape.
    /// Also thrown if the aggregation function is not recognized.
    /// </exception>
    private static void AggregateRange(CoverageRange range, long[] bucketForTime, IReadOnlyDictionary<long, int> bucketIndexes, int bucketCount, string aggregateFunction)
    {
        if (range.Values is null || range.AxisNames is null || range.Shape is null)
        {
            return;
        }

        var timeDimension = range.AxisNames.FindIndex(name => string.Equals(name, "t", StringComparison.OrdinalIgnoreCase));

        // This range is not time-dependent.
        if (timeDimension < 0)
        {
            return;
        }

        if (range.Shape[timeDimension] != bucketForTime.Length)
        {
            throw new InvalidOperationException($"Range '{range.Name}' heeft geen geldige shape voor de t-as.");
        }

        var originalValueCount = 1;

        foreach (var dimensionSize in range.Shape)
        {
            originalValueCount = checked(originalValueCount * dimensionSize);
        }

        if (originalValueCount != range.Values.Count)
        {
            throw new InvalidOperationException($"Range '{range.Name}' heeft een shape die niet overeenkomt met het aantal waarden.");
        }

        var timeStride = 1;

        for (var dimension = timeDimension + 1; dimension < range.Shape.Count; dimension++)
        {
            timeStride = checked(timeStride * range.Shape[dimension]);
        }

        var outputShape = new List<int>(range.Shape);
        outputShape[timeDimension] = bucketCount;

        var outputValueCount = 1;

        foreach (var dimensionSize in outputShape)
        {
            outputValueCount = checked(outputValueCount * dimensionSize);
        }

        var outputValues = new List<double>(outputValueCount);

        for (var outputIndex = 0; outputIndex < outputValueCount; outputIndex++)
        {
            var bucketIndex = outputIndex / timeStride % bucketCount;
            var baseIndex = outputIndex - (bucketIndex * timeStride);
            var count = 0;
            var sum = 0.0;
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;

            for (var timeIndex = 0; timeIndex < bucketForTime.Length; timeIndex++)
            {
                var currentBucket = bucketIndexes[bucketForTime[timeIndex]];

                if (currentBucket != bucketIndex)
                {
                    continue;
                }

                var sourceIndex = baseIndex + timeIndex * timeStride;

                var value = range.Values[sourceIndex];

                if (double.IsNaN(value))
                {
                    continue;
                }

                count++;
                sum += value;

                if (value < minimum)
                {
                    minimum = value;
                }

                if (value > maximum)
                {
                    maximum = value;
                }
            }

            var result = aggregateFunction.ToLowerInvariant() switch
            {
                "count" => count,
                "sum" => count == 0 ? double.NaN : sum,
                "mean" => count == 0 ? double.NaN : sum / count,
                "min" => count == 0 ? double.NaN : minimum,
                "max" => count == 0 ? double.NaN : maximum,
                _ => throw new InvalidOperationException($"Onbekende aggregatiemethode: {aggregateFunction}.")
            };

            outputValues.Add(result);
        }

        range.Values = outputValues;
        range.Shape = outputShape;
    }

    /// <summary>
    /// Slices the provided coverage object based on the temporal constraints defined in the coverage configuration.
    /// </summary>
    /// <param name="coverage">The coverage object to be sliced. Contains domain axes and ranges.</param>
    /// <param name="coverageConfiguration">
    /// The configuration object containing slicing parameters, including the temporal start and end constraints.
    /// </param>
    private static void Slice(Coverage coverage, CoverageConfiguration coverageConfiguration)
    {
        if (coverageConfiguration.SliceBegin == null && coverageConfiguration.SliceEnd == null)
        {
            return;
        }

        if (!coverage.Domain.Axes.TryGetValue("t", out var timeAxis) || timeAxis.Values == null)
        {
            return;
        }

        var keep = new bool[timeAxis.Values.Count];
        var selectedTimes = new List<JsonElement>(timeAxis.Values.Count);

        for (var i = 0; i < timeAxis.Values.Count; i++)
        {
            var time = timeAxis.Values[i].GetDateTimeOffset();
            var afterStart = coverageConfiguration.SliceBegin == null || time >= coverageConfiguration.SliceBegin.Value;
            var beforeEnd = coverageConfiguration.SliceEnd == null || time <= coverageConfiguration.SliceEnd.Value;

            if (!afterStart || !beforeEnd)
            {
                continue;
            }

            keep[i] = true;
            selectedTimes.Add(timeAxis.Values[i]);
        }

        coverage.Domain.Axes["t"] = timeAxis with { Values = selectedTimes };

        foreach (var range in coverage.Ranges.Values)
        {
            SliceRange(range, keep, selectedTimes.Count, timeAxis.Values.Count);
        }
    }

    /// <summary>
    /// Extracts and reshapes data from a given range based on specified time constraints.
    /// </summary>
    /// <param name="range">The range object containing multidimensional data to be sliced.</param>
    /// <param name="keep">Boolean array indicating which time indices should be retained.</param>
    /// <param name="selectedTimeCount">The number of time indices to retain after slicing.</param>
    /// <param name="originalTimeCount">The original total count of time indices before slicing.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the range's shape does not conform to the time axis dimensions.
    /// </exception>
    private static void SliceRange(CoverageRange range, bool[] keep, int selectedTimeCount, int originalTimeCount)
    {
        if (range.Values == null || range.AxisNames == null || range.Shape == null)
        {
            return;
        }

        var timeDimension = range.AxisNames.FindIndex(name => string.Equals(name, "t", StringComparison.OrdinalIgnoreCase));

        if (timeDimension < 0)
        {
            return;
        }

        if (range.Shape[timeDimension] != originalTimeCount)
        {
            throw new InvalidOperationException($"Range '{range.Name}' heeft een ongeldige shape voor de t-as.");
        }

        var timeStride = 1;

        for (var dimension = timeDimension + 1; dimension < range.Shape.Count; dimension++)
        {
            timeStride = checked(timeStride * range.Shape[dimension]);
        }

        var slicedValues = new List<double>();

        for (var flatIndex = 0; flatIndex < range.Values.Count; flatIndex++)
        {
            var timeIndex = flatIndex / timeStride % originalTimeCount;

            if (keep[timeIndex])
            {
                slicedValues.Add(range.Values[flatIndex]);
            }
        }

        range.Values = slicedValues;
        range.Shape[timeDimension] = selectedTimeCount;
    }

    /// <summary>
    /// Retrieves a set of properties to be excluded during materialization,
    /// optionally excluding a specified key property.
    /// </summary>
    /// <param name="idODataKey">The OData key property to potentially exclude from the exclusion set.</param>
    /// <returns>
    /// A read-only set of properties to be excluded from materialisation, or null if no exclusions apply.
    /// </returns>
    private HashSet<string>? GetMaterializerExcludedProperties(string idODataKey)
    {
        if (_excludedProperties.Count == 0)
        {
            return null;
        }

        if (!_excludedProperties.Contains(idODataKey))
        {
            return _excludedProperties;
        }

        if (_excludedProperties.Count == 1)
        {
            return null;
        }

        var materializerExcludedProperties = new HashSet<string>(_excludedProperties, StringComparer.OrdinalIgnoreCase);

        materializerExcludedProperties.Remove(idODataKey);

        return materializerExcludedProperties;
    }

    /// <summary>
    /// Removes properties from the provided data collection that are specified in the excluded properties list.
    /// </summary>
    /// <param name="data">The collection of entities where excluded properties should be removed.</param>
    private void RemoveExcludedProperties(List<Dictionary<string, object?>> data)
    {
        if (_excludedProperties.Count == 0)
        {
            return;
        }

        foreach (var entity in data)
        {
            foreach (var property in _excludedProperties)
            {
                entity.Remove(property);
            }
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
    private static (string Sql, DynamicParameters Params) ConstructDataQuery(ODataToSqlConverter.SqlResult? whereClause, string query, SkipToken.SkipToken? skipToken, ODataQueryOptions queryOptions, string access, string idField, string idFieldEdmType)
    {
        // Validate 'idField' to prevent SQL injection
        if (!IsValidSqlIdentifier(idField))
        {
            throw new ArgumentException($"Invalid SQL identifier: {idField}", nameof(idField));
        }

        // Validate access clause to prevent SQL injection
        ValidateAccessClause(access);

        var dp = new DynamicParameters();

        // Merge existing where parameters (if any)
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
            //  For numeric types, parse the string to the appropriate type
            //  For string/GUID types, keep as string
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
    private static (string Sql, DynamicParameters Params) ConstructCountQuery(ODataToSqlConverter.SqlResult? whereClause, string query, ODataQueryOptions queryOptions, string access)
    {
        // Validate access clause to prevent SQL injection
        ValidateAccessClause(access);

        var dp = new DynamicParameters();

        // Merge existing where parameters (if any)
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
    private static bool IsValidSqlIdentifier(string identifier) => !string.IsNullOrWhiteSpace(identifier) && SqlExpressionRegEx().IsMatch(identifier);

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

    /// <summary>
    /// Represents an aggregation period, which may be defined as either a fixed duration
    /// or a number of calendar months. This struct is used to encapsulate these two types
    /// of aggregation periods in a unified representation.
    /// </summary>
    private readonly record struct AggregationPeriod(TimeSpan? FixedDuration, int? CalendarMonths);

    /// <summary>
    /// Parses a string representation of an aggregation period into an AggregationPeriod struct,
    /// which may represent either a fixed duration or a number of calendar months.
    /// </summary>
    /// <param name="value">The input string specifying the aggregation period.
    /// This can be an ISO 8601 duration format string representing a fixed duration
    /// or a specific calendar-based period (e.g., "P3M" for three months).</param>
    /// <returns>An <c>AggregationPeriod</c> struct containing the parsed duration or calendar months.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the input string is invalid, contains unsupported combinations of calendar and fixed durations,
    /// or specifies a period less than or equal to zero.
    /// </exception>
    private static AggregationPeriod ParseAggregationPeriod(string value)
    {
        value = value.Trim();

        // Pure kalender-perioden.
        if (PeriodRegex().IsMatch(value))
        {
            var amount = int.Parse(value[1..^1], CultureInfo.InvariantCulture);

            if (amount <= 0)
            {
                throw new InvalidOperationException("De aggregatieperiode moet groter zijn dan nul.");
            }

            var calendarMonths = value[^1] switch
            {
                'M' => amount,
                'Y' => checked(amount * 12),
                _ => throw new InvalidOperationException()
            };

            return new AggregationPeriod(FixedDuration: null, CalendarMonths: calendarMonths);
        }

        var datePartEnd = value.IndexOf('T');
        var datePart = datePartEnd >= 0 ? value[..datePartEnd] : value;

        if (datePart.Contains('Y') || datePart.Contains('M'))
        {
            throw new InvalidOperationException("Gecombineerde kalender- en vaste durations worden niet ondersteund.");
        }

        var duration = XmlConvert.ToTimeSpan(value);

        if (duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("De aggregatieperiode moet groter zijn dan nul.");
        }

        return new AggregationPeriod(FixedDuration: duration, CalendarMonths: null);
    }

    /// <summary>
    /// Calculates a fixed bucket index for a given point in time based on the specified duration and anchor time.
    /// </summary>
    /// <param name="times">An array of timestamps representing the available time points.</param>
    /// <param name="time">The current point in time for which the bucket index is to be calculated.</param>
    /// <param name="duration">The fixed duration that defines the size of each bucket.</param>
    /// <returns>The index of the bucket corresponding to the given point in time.</returns>
    private static long GetFixedBucket(DateTimeOffset[] times, DateTimeOffset time, TimeSpan duration)
    {
        var anchor = times.Min();
        var elapsed = time - anchor;

        return elapsed.Ticks / duration.Ticks;
    }

    /// <summary>
    /// Calculates the start time for a fixed-duration bucket based on the provided times, bucket index, and duration.
    /// </summary>
    /// <param name="times">An array of time values used to determine the anchor time.</param>
    /// <param name="bucket">The index of the bucket for which the start time is being calculated.</param>
    /// <param name="duration">The duration of each bucket.</param>
    /// <returns>The calculated start time of the specified bucket as a DateTimeOffset.</returns>
    private static DateTimeOffset GetFixedBucketStart(DateTimeOffset[] times, long bucket, TimeSpan duration)
    {
        var anchor = times.Min();

        return anchor.AddTicks(checked(bucket * duration.Ticks));
    }

    /// <summary>
    /// Determines the calendar bucket for a given date-time based on the specified number of calendar months.
    /// </summary>
    /// <param name="time">The date and time to assign to a calendar bucket.</param>
    /// <param name="calendarMonths">The duration of each bucket in calendar months.</param>
    /// <returns>The calculated calendar bucket as a long integer.</returns>
    private static long GetCalendarBucket(DateTimeOffset time, int calendarMonths)
    {
        var absoluteMonth = (long)time.Year * 12 + time.Month - 1;

        return absoluteMonth / calendarMonths;
    }

    /// <summary>
    /// Calculates the start date of a calendar-based bucket.
    /// </summary>
    /// <param name="bucket">The bucket index representing a period of time.</param>
    /// <param name="calendarMonths">The number of calendar months in each bucket.</param>
    /// <returns>The start date and time of the specified bucket.</returns>
    private static DateTimeOffset GetCalendarBucketStart(long bucket, int calendarMonths)
    {
        var absoluteMonth = checked(bucket * calendarMonths);

        var year = checked((int)(absoluteMonth / 12));
        var month = checked((int)(absoluteMonth % 12) + 1);

        return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Regular expression used to validate and match specific period formats in the format of ISO-8601 duration formats,
    /// where the duration is expressed as months or years.
    /// </summary>
    /// <returns>A compiled instance of a <see cref="Regex"/>.</returns>
    [GeneratedRegex(@"^P\d+[MY]$")]
    private static partial Regex PeriodRegex();

    /// <summary>
    /// Provides a regular expression pattern to validate SQL expressions, ensuring
    /// they consist of valid characters such as alphanumeric, underscores, and dots,
    /// starting with a letter or underscore.
    /// </summary>
    /// <returns>A regular expression for validating SQL expressions.</returns>
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_\.]*$")]
    private static partial Regex SqlExpressionRegEx();
}
