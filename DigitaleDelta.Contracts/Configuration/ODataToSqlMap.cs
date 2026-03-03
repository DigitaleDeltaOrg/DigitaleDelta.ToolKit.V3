using System.Data.Common;

namespace DigitaleDelta.Contracts.Configuration;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

/// <summary>
/// Binds OData query parameters and their database equivalents.
/// </summary>
public record ODataToSqlMap
{
    /// <summary>
    /// Property name in OData, e.g. "Result", "PhenomenonTime/BeginPosition", "Foi/Code", "Parameter/Quantity", etc.
    /// </summary>
    public required string ODataPropertyName { get; init; }

    /// <summary>
    /// Name of the SQL column, e.g. "result", "phenomenon_time_begin_position", "foi_code", "parameter_quantity", etc.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Type of the OData property, e.g. "Edm.String", "Edm.Int32", "Edm.DateTimeOffset", etc.
    /// </summary>
    public required string EdmType { get; init; }

    /// <summary>
    /// Represents the name of the database column corresponding to the OData property.
    /// </summary>
    public required string ColumnName { get; init; }

    /// <summary>
    /// Overrides the SQL expression used in WHERE clauses when the selected output expression differs,
    /// e.g. "SELECT result AS value FROM foo WHERE result = 'bar'".
    /// </summary>
    public string? WhereClausePart { get; init; }

    /// <summary>
    /// A delegate function used to retrieve a value from a database reader based on the given ordinal.
    /// It serves as a custom accessor for specific fields within a database result set.
    /// The function takes a <see cref="DbDataReader"/> and an integer representing the column ordinal, and returns an object or null.
    /// </summary>
    public Func<DbDataReader, int, object?>? Getter { set; get; }

    /// <summary>
    /// Indicates whether this property is disallowed in filter expressions.
    /// When true, no SQL query part will be generated for this property in filter clauses.
    /// </summary>
    public bool DisallowInFilter { get; set; }

    /// <summary>
    /// Indicates whether this property should be excluded from OData responses.
    /// When true, the property will be fetched from the database but not included in the response.
    /// Useful for internal fields like pagination IDs that are needed for queries but not for clients.
    /// </summary>
    public bool ExcludeFromResponse { get; set; }
}
