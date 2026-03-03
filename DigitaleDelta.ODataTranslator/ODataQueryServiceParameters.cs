using System.Data.Common;
using DigitaleDelta.Contracts.Configuration;

namespace DigitaleDelta.ODataTranslator;

/// <summary>
/// Represents parameters used for OData query translations and mapping to SQL queries.
/// </summary>
public class ODataQueryServiceParameters
{
    /// <summary>
    /// Represents the mapping of OData properties to their corresponding SQL query components.
    /// </summary>
    /// <remarks>
    /// This property provides a dictionary that binds OData property names to their corresponding
    /// mappings in the SQL query. Each entry in the dictionary consists of a key as the OData property name
    /// and a value as an <see cref="ODataToSqlMap"/> object, which specifies details about the mapping.
    /// </remarks>
    public required Dictionary<string, ODataToSqlMap> PropertyMap { init; get; }

    /// <summary>
    /// Represents the mapping of OData functions to their corresponding SQL function formats.
    /// </summary>
    /// <remarks>
    /// This property contains a dictionary that associates OData function names with their SQL equivalents.
    /// Each entry in the dictionary consists of a key as the OData function name and a value as an
    /// <see cref="ODataFunctionMap"/> object, which provides details about the SQL function format, expected
    /// argument types, return type, and optional wildcard considerations.
    /// </remarks>
    public required Dictionary<string, ODataFunctionMap> FunctionMap { init; get; }

    /// <summary>
    /// Represents the secret token used for authentication or secure communication purposes.
    /// </summary>
    /// <remarks>
    /// This property is required and stores a cryptographic key or secret that may be used
    /// for hashing, signing, or other verification mechanisms in conjunction with the specified
    /// <see cref="HmacAlgorithm"/>. Proper handling and storage of this value are crucial to
    /// ensure security in the application's operations.
    /// </remarks>
    public required string TokenSecret { init; get; }

    /// <summary>
    /// Specifies the algorithm used for generating HMAC (Hash-based Message Authentication Code) signatures.
    /// </summary>
    /// <remarks>
    /// This property determines which HMAC algorithm is used when performing cryptographic operations
    /// to ensure the authenticity and integrity of data. It must be set to a valid HMAC algorithm name,
    /// such as "HMACSHA256" by default or any other supported algorithm depending on the cryptographic needs.
    /// </remarks>
    public string HmacAlgorithm { set; get; } = "HMACSHA256";

    /// <summary>
    /// Provides a reverse mapping of database column names to their corresponding OData properties.
    /// </summary>
    /// <remarks>
    /// This property generates a dictionary that maps database column names (as keys) to their corresponding
    /// <see cref="ODataToSqlMap"/> objects (as values). The reverse mapping is constructed dynamically
    /// based on the <see cref="PropertyMap"/>, using the ColumnName property if specified, otherwise the ODataPropertyName.
    /// The keys in the dictionary are treated in a case-insensitive manner.
    /// This allows SQL queries to use ODataPropertyName as column aliases (e.g., AS "Parameter/Quantity").
    /// </remarks>
    public Dictionary<string, ODataToSqlMap> ReversePropertyMap
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            var reverse = new Dictionary<string, ODataToSqlMap>(PropertyMap.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var @field in PropertyMap.Select(kvp => kvp.Value))
            {
                var columnName = !string.IsNullOrWhiteSpace(@field.ColumnName) ? @field.ColumnName : @field.ODataPropertyName;

                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    reverse.TryAdd(columnName, @field);
                }
            }

            field = reverse;

            return field;
        }
    }

    /// <summary>
    /// Provides a factory function responsible for creating new instances of <see cref="DbConnection"/>.
    /// </summary>
    /// <remarks>
    /// This property is used to manage database connection lifecycles within the service.
    /// It returns an instance of a <see cref="DbConnection"/> when invoked, ensuring that each database query or transaction
    /// operates on a dedicated connection. The function must be configured to initialize the appropriate database connection
    /// based on the application's requirements.
    /// </remarks>
    public required Func<DbConnection> ConnectionFactory { init; get; }

    /// <summary>
    /// Specifies the name of the field that uniquely identifies a record in the database.
    /// </summary>
    /// <remarks>
    /// This property is used to define the primary key field, which enables mapping and filtering
    /// operations within the SQL translation logic. It is critical for ensuring correct record
    /// identification and manipulation in queries.
    /// </remarks>
    public required string IdField { init; get; }

    /// <summary>
    /// Represents the SQL query used to retrieve data in response to an OData request.
    /// </summary>
    /// <remarks>
    /// This property defines the base query string executed to fetch data from the database.
    /// It is typically constructed based on the parameters provided in the OData request and may include
    /// conditions, joins, and other SQL elements necessary for the specific translation of the OData query to SQL semantics.
    /// </remarks>
    public required string DataQuery { init; get; }

    /// <summary>
    /// Represents the SQL query used for counting the total number of records in the dataset
    /// retrieved based on the OData query parameters.
    /// </summary>
    /// <remarks>
    /// This property is essential for translating the OData count functionality into its SQL equivalent.
    /// It provides the SQL query template or statement executed to determine the total
    /// number of matching records in the data source.
    /// </remarks>
    public required string CountQuery { init; get; }

    /// <summary>
    /// Represents the name of the entity set being queried.
    /// </summary>
    /// <remarks>
    /// This property is used to specify the name of the OData entity set that the queries are targeting.
    /// It is a required parameter that helps in mapping OData operations to their corresponding
    /// database tables or views within the context of a query translation.
    /// </remarks>
    public required string EntitySetName { init; get; }

    /// <summary>
    /// Defines the time period, in minutes, for which the count query results should be cached.
    /// </summary>
    /// <remarks>
    /// This property controls the duration for caching the results of count queries executed against the database.
    /// A higher value will reduce the frequency of count query executions but may result in less up-to-date counts.
    /// It is recommended to configure this value based on the application's acceptable balance between data freshness and performance.
    /// </remarks>
    public int CountCachePeriod { init; get; } = 5;

    /// <summary>
    /// Defines the maximum number of items that can be included on a single page of query results.
    /// </summary>
    /// <remarks>
    /// This property specifies the limit for the number of records to be returned in a single query page. It is useful for
    /// controlling data retrieval size and preventing excessively large query responses. If the requested page size exceeds
    /// this value, the maximum specified here will be enforced.
    /// </remarks>
    public int MaxPageSize { init; get; }

    /// <summary>
    /// Specifies the default number of records returned per page in OData queries when no page size is explicitly defined.
    /// </summary>
    /// <remarks>
    /// This property determines the fallback page size for paginated OData results. It is useful for ensuring consistent paging
    /// behavior when clients do not specify a custom page size or when the requested page size is not applicable.
    /// </remarks>
    public int DefaultPageSize { init; get; }
}
