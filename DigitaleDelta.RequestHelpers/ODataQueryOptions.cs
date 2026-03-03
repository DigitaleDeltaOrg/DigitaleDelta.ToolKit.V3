// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace DigitaleDelta.RequestHelpers;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

/// <summary>
/// Represents the set of OData query parameters that can be applied to filter
/// and control the data retrieved from an HTTP request.
/// </summary>
public class ODataQueryOptions
{
    /// Gets or sets the filtering criteria for the query.
    /// This property allows specifying a condition or set of conditions
    /// to restrict the data returned in a query response.
    /// Using this property helps refine query results based on
    /// specific requirements, such as filtering by attributes or values.
    public ODataParser.FilterOptionContext? Filter { get; set; }

    /// Gets or sets the maximum number of items to be retrieved in the query.
    /// This property defines the upper limit on the number of records to be returned
    /// and directly impacts the data fetch operation. If the specified value is less
    /// than 1 or exceeds the allowable maximum, an exception may be raised.
    /// Assigning this property helps control the size of the query response.
    public int Top { get; set; }

    /// Gets or sets a value indicating whether the total count of items should be included
    /// in the response. When set to true, the data returned will include the count of
    /// matching entities, which can help in client-side data handling or pagination
    /// logic. If this property is null or false, the count will not be explicitly retrieved.
    public bool? Count { get; set; }

    /// Gets or sets the requested maximum number of results to be returned in the query.
    /// This property represents the value specified by the $top query option, allowing
    /// the client to limit the number of items in the response. If the value is outside
    /// the acceptable range defined by the implementation, an exception may be thrown.
    /// A null value indicates that the client requests no explicit limit.
    public int? RequestedTop { set; get; }

    /// Gets or sets the Coordinate Reference System (CRS) identifier.
    /// This property defines the reference system used for spatial data queries
    /// to ensure that the geometries and spatial filters are applied in the correct spatial reference context.
    /// If not explicitly set, a default value may be used depending on the implementation.
    public int? CrsId { set; get; }

    /// Gets or sets a value indicating whether null values should be omitted from the query results.
    /// When set to true, null values are excluded during the materialisation of data, ensuring only non-null fields are included in the results.
    /// This property can be useful for optimising payload size and enhancing data processing efficiency in certain scenarios.
    public bool OmitNulls { set; get; }

    /// Gets or sets the user identifier associated with the current query context.
    /// This property represents the identity of the user making the request,
    /// which can be used for authentication, authorisation, and logging purposes.
    /// The value is typically derived from the user's security claims or context.
    public ClaimsPrincipal? ClaimsPrincipal { set; get; }

    /// Gets or sets the URL associated with the query options.
    /// This property represents the full request URL, which can be used to process
    /// or manipulate OData queries, including extracting additional parameters or
    /// constructing further related queries.
    /// If not explicitly set, it may remain null and should be handled appropriately
    /// in consuming logic to avoid null reference issues.
    public string? Url { init; get; }

    /// Gets or sets the skip token used to facilitate server-side pagination in OData queries.
    /// This property represents a token that enables retrieving the next set of items in a query result.
    /// It typically preserves the state of the last retrieved item, allowing sequential data access
    /// without requiring the client to manage offsets or indexing.
    public string? SkipToken { get; set; }

    /// Gets or sets the selection criteria for the query.
    /// This property allows specifying specific fields or properties
    /// to include in the response, enabling control over which data
    /// elements are returned in the query result.
    public string?[]? Select { get; set; }
}
