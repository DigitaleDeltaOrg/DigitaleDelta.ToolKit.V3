using DigitaleDelta.Contracts;
using DigitaleDelta.Contracts.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DigitaleDelta.ODataTranslator;

namespace DigitaleDelta.DbRowMaterializer;

/// <summary>
/// Provides methods for transforming database query results into collections of materialized objects.
/// This utility is designed to handle data mapping between database rows and .NET collections,
/// with features such as field mapping, null suppression, and optional spatial reference handling.
/// </summary>
public static class DbRowMaterializer
{
    private static readonly WKBReader _wkbReader = new();

    private enum ColKind : byte
    {
        @string,
        int32,
        int64,
        @decimal,
        boolean,
        dateTimeOffset,
        geography,
        fallbackString
    }

    private enum RenderKind : byte
    {
        @default,
        json
    }

    private const string renderAsJson = "json";

    /// <summary>
    /// Reads rows from the given <see cref="DbDataReader"/> and maps them to a list of dictionaries
    /// where each dictionary represents a single row with keys as column names and values as the mapped data.
    /// </summary>
    /// <param name="reader">The data reader containing the rows to read and map.</param>
    /// <param name="fieldMaps">A mapping of OData property names to SQL column specifications.</param>
    /// <param name="suppressNulls">Determines whether null values should be excluded from the resulting dictionaries.</param>
    /// <param name="lineCount">The maximum number of rows to materialise. Must be greater than zero.</param>
    /// <param name="logger">The logger for capturing logs during the materialisation process.</param>
    /// <param name="targetSrid">The optional Spatial Reference Identifier (SRID) for any geometry or geography data in the output.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="excludeProperties">An optional set of OData property names to be excluded from the mapping process.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains a list of dictionaries, each representing a mapped row.</returns>
    public static async Task<List<Dictionary<string, object?>>> MaterializeToListAsync(
        DbDataReader reader,
        Dictionary<string, ODataToSqlMap> fieldMaps,
        bool suppressNulls,
        int lineCount,
        ILogger logger,
        int? targetSrid = null,
        IReadOnlySet<string>? excludeProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(fieldMaps);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lineCount, 0);

        var ordinals = BuildNameMapper(reader);
        var tempPaths = new List<string>(reader.FieldCount);
        var tempSpecs = new List<ColSpec>(reader.FieldCount);

        ExtractColumnSpecifications(fieldMaps, ordinals, tempPaths, tempSpecs, logger, excludeProperties);

        var paths = tempPaths.ToArray();
        var specs = tempSpecs.ToArray();
        var count = paths.Length;
        var fieldTypes = new Type[count];

        for (var i = 0; i < count; i++)
        {
            try
            {
                fieldTypes[i] = reader.GetFieldType(specs[i].Ordinal);
            }
            catch (InvalidCastException)
            {
                fieldTypes[i] = specs[i].Kind == ColKind.geography ? typeof(byte[]) : typeof(object);
            }
        }

        var result = new List<Dictionary<string, object?>>(lineCount);
        var rowCount = 0;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, object?>(count, StringComparer.Ordinal);

            for (var i = 0; i < count; i++)
            {
                var val = ReadValueFast(logger, reader, specs[i], fieldTypes[i], targetSrid);

                if (!suppressNulls || (val is not null && val is not DBNull))
                {
                    row.Add(paths[i], val);
                }
            }

            result.Add(row);
            rowCount++;

            if (rowCount >= lineCount)
            {
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Reads a value from the specified column in the <see cref="DbDataReader"/> efficiently, applying transformations based on the column type and target field type.
    /// </summary>
    /// <param name="logger">The logger used for capturing diagnostic information during value retrieval.</param>
    /// <param name="r">The data reader instance from which the value is read.</param>
    /// <param name="spec">The column specification that includes the ordinal position and type of the column.</param>
    /// <param name="fieldType">The expected data type of the target field.</param>
    /// <param name="targetSrid">The optional Spatial Reference Identifier (SRID) for interpreting geography data, if applicable.</param>
    /// <returns>Returns the value retrieved from the specified column. The type and format of the returned value depend on the column type and transformations applied.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ReadValueFast(ILogger logger, DbDataReader r, ColSpec spec, Type fieldType, int? targetSrid)
    {
        var ord = spec.Ordinal;

        if (r.IsDBNull(ord))
        {
            return null;
        }

        if (spec.Render == RenderKind.json)
        {
            var raw = r.GetValue(ord) as string;

            return string.IsNullOrWhiteSpace(raw) ? null : new RawJson(raw);
        }

        switch (spec.Kind)
        {
            case ColKind.@string:
                var val = r.GetValue(ord);

                return val switch
                {
                    DBNull => null,
                    string v1 => string.IsNullOrWhiteSpace(v1) ? null : v1,
                    _ => val
                };

            case ColKind.int32:
                return fieldType == typeof(int) ? r.GetFieldValue<int>(ord) : Convert.ToInt32(r.GetValue(ord));

            case ColKind.int64:
                return fieldType == typeof(long) ? r.GetFieldValue<long>(ord) : Convert.ToInt64(r.GetValue(ord));

            case ColKind.@decimal:
                return fieldType == typeof(decimal) ? r.GetFieldValue<decimal>(ord) : Convert.ToDecimal(r.GetValue(ord));

            case ColKind.boolean:
                return fieldType == typeof(bool) ? r.GetFieldValue<bool>(ord) : Convert.ToBoolean(r.GetValue(ord));

            case ColKind.dateTimeOffset:
                {
                    var t = fieldType;
                    if (t == typeof(DateTimeOffset))
                    {
                        return r.GetFieldValue<DateTimeOffset>(ord);
                    }

                    if (t == typeof(DateTime))
                    {
                        try
                        {
                            var dt = r.GetFieldValue<DateTime>(ord);

                            return new DateTimeOffset(dt);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            logger.LogWarning(ex, "Invalid date value in column {Ordinal}, returning null", ord);

                            return null;
                        }
                    }

                    if (t != typeof(string))
                    {
                        return r.GetValue(ord);
                    }

                    var s = r.GetFieldValue<string>(ord);

                    return DateTimeOffset.TryParse(s, out var parsed) ? parsed : s;
                }

            case ColKind.geography:
                return GeoFieldValue(targetSrid, r, ord);

            default:
                {
                    var o = r.GetValue(ord);

                    if (o is string str)
                    {
                        return string.IsNullOrWhiteSpace(str) ? null : str;
                    }

                    return o;
                }
        }
    }

    /// <summary>
    /// Extracts column specifications from the provided OData to SQL mappings and populates the specified temporary paths and specifications.
    /// This method maps OData property names to their respective SQL column ordinals and types, while also applying filtering for excluded properties.
    /// </summary>
    /// <param name="fieldMaps">A dictionary of OData property names mapped to SQL column specifications.</param>
    /// <param name="ordinals">A dictionary mapping SQL column names to their corresponding ordinals in the <see cref="DbDataReader"/>.</param>
    /// <param name="tempPaths">A list to store the mapped OData property names.</param>
    /// <param name="tempSpecs">A list to store the column specifications, including ordinals and types.</param>
    /// <param name="logger">The logger used for logging errors or messages during the extraction process.</param>
    /// <param name="excludeProperties">An optional set of OData property names to be excluded from the mapping process.</param>
    private static void ExtractColumnSpecifications(
        Dictionary<string, ODataToSqlMap> fieldMaps,
        Dictionary<string, int> ordinals,
        List<string> tempPaths,
        List<ColSpec> tempSpecs,
        ILogger logger,
        IReadOnlySet<string>? excludeProperties = null)
    {
        foreach (var fm in fieldMaps.Values)
        {
            var oDataPropertyName = fm.ODataPropertyName.Trim();

            if (excludeProperties?.Contains(oDataPropertyName) == true)
            {
                continue;
            }

            var columnName = fm.ColumnName.Trim();

            if (!ordinals.TryGetValue(columnName, out var ord))
            {
                logger.LogError("Column {Column} not found in result set", columnName);

                continue;
            }

            var kind = MapEdmToKind(fm.EdmType);
            var render = MapRenderAs(fm.RenderAs);

            tempPaths.Add(oDataPropertyName);
            tempSpecs.Add(new ColSpec(ord, kind, render));
        }
    }

    /// <summary>
    /// Builds a mapping of column names to their ordinal positions in the provided <see cref="DbDataReader"/> object.
    /// </summary>
    /// <param name="reader">The data reader containing the column names and ordinal values.</param>
    /// <returns>Returns a dictionary where the keys are column names and the values are their corresponding ordinal positions.</returns>
    private static Dictionary<string, int> BuildNameMapper(DbDataReader reader)
    {
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);

            ordinals[name] = i;
        }

        return ordinals;
    }

    /// <summary>
    /// Represents a column specification for database query result mapping.
    /// Defines the ordinal position of the column, its associated data type, and an optional
    /// presentation hint that overrides the default response rendering for the column.
    /// </summary>
    private readonly struct ColSpec(int ordinal, ColKind kind, RenderKind render)
    {
        public readonly int        Ordinal = ordinal;
        public readonly ColKind    Kind    = kind;
        public readonly RenderKind Render  = render;
    }

    /// <summary>
    /// Maps the optional <c>RenderAs</c> hint from a property mapping to the internal render-kind enumeration.
    /// </summary>
    /// <param name="renderAs">The raw <c>RenderAs</c> string from configuration, or <c>null</c> if absent.</param>
    /// <returns><see cref="RenderKind.json"/> when the hint requests JSON rendering; <see cref="RenderKind.@default"/> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RenderKind MapRenderAs(string? renderAs)
    {
        if (string.IsNullOrWhiteSpace(renderAs))
        {
            return RenderKind.@default;
        }

        return string.Equals(renderAs.Trim(), renderAsJson, StringComparison.OrdinalIgnoreCase)
            ? RenderKind.json
            : RenderKind.@default;
    }

    /// <summary>
    /// Maps an EDM (Entity Data Model) type string to a corresponding internal column kind enumeration.
    /// </summary>
    /// <param name="edmType">The EDM type string, which represents the data type in OData notation, to be mapped to a corresponding column kind.</param>
    /// <returns>The internal <c>ColKind</c> enumeration value that matches the provided EDM type, or <c>FallbackString</c> if no explicit match is found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ColKind MapEdmToKind(string? edmType)
    {
        var norm = string.IsNullOrWhiteSpace(edmType) ? "edm.string" : edmType.Trim().ToLowerInvariant();

        return norm switch
        {
            "edm.string" => ColKind.@string,
            "edm.int32" => ColKind.int32,
            "edm.int64" or "edm.long" => ColKind.int64,
            "edm.decimal" => ColKind.@decimal,
            "edm.boolean" => ColKind.boolean,
            "edm.datetimeoffset" => ColKind.dateTimeOffset,
            "edm.geography" => ColKind.geography,
            _ => ColKind.fallbackString
        };
    }

    /// <summary>
    /// Retrieves a geography field value from the specified database reader, optionally transforming its spatial reference system identifier (SRID).
    /// </summary>
    /// <param name="targetSrid">The target spatial reference system identifier (SRID). If null, no transformation is applied.</param>
    /// <param name="r">The database reader to retrieve the geographic data from.</param>
    /// <param name="ord">The ordinal position of the geography field value in the reader.</param>
    /// <returns>Returns a <see cref="Geometry"/> object representing the geography field value, or null if the value is not present or cannot be read.</returns>
    private static Geometry? GeoFieldValue(int? targetSrid, DbDataReader r, int ord)
    {
        if (r.IsDBNull(ord))
        {
            return null;
        }

        var length = r.GetBytes(ord, 0, null, 0, 0);

        if (length == 0)
        {
            return null;
        }

        var bytes = new byte[length];

        r.GetBytes(ord, 0, bytes, 0, (int)length);

        try
        {
            var geom = _wkbReader.Read(bytes);

            if (geom == null)
            {
                return null;
            }

            if (targetSrid is not > 0 || geom.SRID == targetSrid.Value)
            {
                return ODataTranslator.Helpers.CrsHelper.Round(geom, geom.SRID);
            }

            var (ok, transformed) = ODataTranslator.Helpers.CrsHelper.TransformGeometry(targetSrid.Value, geom);

            geom = ok && transformed != null
                ? transformed
                : geom;

            if (!ok || transformed == null)
            {
                geom.SRID = targetSrid.Value;
                geom = ODataTranslator.Helpers.CrsHelper.Round(geom, targetSrid.Value);
            }

            return geom;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a reverse-ordered map of properties from the database reader based on the specified query parameters.
    /// </summary>
    /// <param name="reader">The database reader object containing data to be processed.</param>
    /// <param name="parameters">The parameters containing the reverse property mapping configuration.</param>
    /// <returns>A collection of <see cref="ODataToSqlMap"/> objects representing the reverse ordered mapped properties.</returns>
    public static IEnumerable<ODataToSqlMap> CreateReverseOrderedMap(DbDataReader reader, ODataQueryServiceParameters parameters)
    {
        var orderedPropertyMaps = new List<ODataToSqlMap>(reader.FieldCount);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var colName = reader.GetName(i);

            if (parameters.ReversePropertyMap.TryGetValue(colName, out var map))
            {
                orderedPropertyMaps.Add(map);
            }
        }

        return orderedPropertyMaps;
    }
}
