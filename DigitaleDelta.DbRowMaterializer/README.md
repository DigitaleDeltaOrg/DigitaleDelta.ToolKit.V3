# DigitaleDelta.DbRowMaterializer

A high-performance .NET library for projecting database rows to dynamic dictionaries with flexible, schema-driven mapping.  
Designed for OData-style APIs, it efficiently materializes data from a `DbDataReader` to dictionaries for direct JSON serialization—supporting property suppression and geospatial types (using NetTopologySuite).

This library is often used together with [`DigitaleDelta.ODataTranslator`](https://www.nuget.org/packages/DigitaleDelta.ODataTranslator) to bridge OData-to-SQL mapping, but can be applied anywhere you need dynamic, high-speed tabular data extraction.

---

## Features

- **Dynamic materialization**: Project arbitrary columns to `Dictionary<string, object?>` per call; no fixed model mapping required.
- **Null suppression**: Optionally omit null or empty results for concise JSON output.
- **Supports OData-to-SQL mapping** via `ODataToSqlMap`.
- **Robust geospatial support** with fast WKB decoding (via NetTopologySuite).
- **Efficient processing**: No reflection, no boxing/unboxing overhead for common types.
- **Async and cancellation support**: Fully async with `CancellationToken`.
- **Minimal dependencies**: Only depends on `DigitaleDelta.ODataTranslator` (for mappings) and NetTopologySuite.

---

## Usage

```csharp 
// given: a DbDataReader, IEnumerablefor the desired projection, and (optional) target SRID for geospatial fields
var rows = await DbRowMaterializer.MaterializeToListAsync(dbReader, projection, srid: 4326);
```


Each dictionary returned represents a row, containing only the requested (and non-null) properties keyed by their OData property name.

---

## How it works

- Uses the provided column mappings (`ODataToSqlMap`) to extract only relevant columns, map them to output property names, and optimally convert values (string, int, bool, decimal, DateTimeOffset, or NetTopologySuite geographies).
- Skips all null/empty string values when `suppressNulls` is set, for leaner output.
- Handles geospatial columns (WKB) out of the box.
- Designed for large result sets and high-throughput APIs: processes database rows as fast as native .NET allows.

---

## Dependencies

- [DigitaleDelta.ODataTranslator](https://www.nuget.org/packages/DigitaleDelta.ODataTranslator)
- [NetTopologySuite](https://www.nuget.org/packages/NetTopologySuite)

---

## Common Scenario

This library is typically used in OData-to-SQL powered APIs where projections (property lists) differ per request and performance is critical, e.g.:

```csharp 
// given: a DbDataReader, IEnumerablefor the desired projection, and (optional) target SRID for geospatial fields
var rows = await DbRowMaterializer.MaterializeToListAsync(dbReader, projection, srid: 4326);
```


---

## Testing

This project uses XUnit for unit and integration tests. Tests cover materialization, type conversions, geospatial handling, and null suppression.

### Running tests

```bash 
  dotnet test
``` 


---

## Contributing

Pull requests, improvements, and feedback are welcome!  
For guidelines, please see CONTRIBUTING.md.

---

## License

MIT License. See LICENSE for details.
