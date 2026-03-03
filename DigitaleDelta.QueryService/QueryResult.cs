// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.QueryService;

/// <summary>
/// Result of a query execution.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// Data returned by the query as a collection of dictionaries,
    /// </summary>
    public IEnumerable<Dictionary<string, object?>>? Data { get; init; }
    /// <summary>
    /// OData skip token for pagination to retrieve the next page of results.
    /// </summary>
    public string? SkipToken { get; init; }
    /// <summary>
    /// Total count of items available for the query, if requested.
    /// </summary>
    public int? TotalCount { get; init; }
    /// <summary>
    /// Indicates whether there is more data available beyond the paged result set.
    /// </summary>
    public bool MoreData { get; init; }
    /// <summary>
    /// Indicates whether an error occurred during query execution.
    /// </summary>
    public bool HasError { get; set; }
    /// <summary>
    /// Detailed information about any error that occurred during the execution of the query.
    /// </summary>
    public string? ErrorDetails { get; set; }
}
