# Digitale Delta - CSharp Libraries

## Extensibility: Plugging in Authorization and Logging

The DigitaleDelta API V3 stack takes care of all OData translation, OM&S mapping, authentication, result materialization and query processing via its own modular, highly-configurable NuGet packages.

**All you have to implement yourself are:**
- **Authorization (access filtering):** restricts data on a per-user/tenant/policy basis.
- **Request logging:** log incoming requests, responses, performance, and (optional) audit details.

### How does it work?

#### 1. **Authorization**

Your implementation should realize the `IAuthorization` interface:

```csharp 
public interface IAuthorization 
{
  Task<bool> TaskTryAuthorizeAsync(ClaimsPrincipal? claimsPrincipal, out string access);
}
```

- The API layer will call your implementation for each request.
- You return `true` (authorized) and attach an SQL (or OData) WHERE fragment in `access` **or** return `false` to block unauthorized requests.
- All standard claims (from API key, mTLS, OIDC/JWT, etc.) are always available in the provided `ClaimsPrincipal`.

##### _Example: allow only access to own organization_

```csharp 
public class OrgAuthorization : IAuthorization 
{
    public TaskTryAuthorizeAsync(ClaimsPrincipal? user, out string access) 
    {
        var orgId = user?.FindFirst("organizationId")?.Value; 
        if (!string.IsNullOrEmpty(orgId)) 
        {
            access = $"organization_id = '{orgId}'"; 
            return Task.FromResult(true);
        } access = null!; 
        return Task.FromResult(false);
    }
}
```

Register your implementation via DI:
```csharp 
builder.Services.AddScoped<IAuthorization, OrgAuthorization>();
```

---

#### 2. **Request logging**

Plug in your implementation for `IRequestLogger`—log as simply or extensively as needed:

```csharp 
public interface IRequestLogger 
{
    void LogRequest(string url, ClaimsPrincipal? user, string requestId, DateTime requestStart); 
    void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null);
}
```


csharp public class OrgAuthorization : IAuthorization { public TaskTryAuthorizeAsync(ClaimsPrincipal? user, out string access) { var orgId = user?.FindFirst("organizationId")?.Value; if (!string.IsNullOrEmpty(orgId)) { access = $"organization_id = '{orgId}'"; return Task.FromResult(true); } access = null!; return Task.FromResult(false); } }```

Register your implementation via DI:

```csharp 
builder.Services.AddScoped<IAuthorization, OrgAuthorization>();
```

---

#### 2. **Request logging**

Plug in your implementation for `IRequestLogger`—log as simply or extensively as needed:

```csharp 
public interface IRequestLogger 
{
    void LogRequest(string url, ClaimsPrincipal? user, string requestId, DateTime requestStart); 
    void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null);
}


```

Example (log to console):
```csharp
public void LogResponse(string requestId, bool succeeded, DateTimeOffset requestEnd, TimeSpan duration, int? responseSize, string? message = null)
    => Console.WriteLine($"[{requestEnd}] Response {requestId}, succeeded={succeeded}, {duration.TotalMilliseconds}ms, size={responseSize ?? 0}");
}
```

---

### What is done for you by the DigitaleDelta packages?

- OData to SQL translation and validation (with configurable mappings)
- Dynamic property projection and null suppression
- Full authentication (API keys, JWT/OIDC, mTLS, ...)
- Query and pagination helpers (e.g. skip tokens)
- Error standardization according to DD API v3
- Caching and materialization
- CSDL/OAS-driven contract

---

**You only need to:**
- Implement/configure authorization (if custom filtering is required)
- Implement/configure request/response logging
- Adapt CSDL/OAS and OData-to-SQL mappings for your specific API/domain

Everything else is plug-and-play, resolved via configuration and registration of the provided NuGet packages.

---

## Database Support

The DigitaleDelta framework is database-agnostic by design. All database-specific SQL is configured in JSON mapping files, not hardcoded in the framework.

### Supported Databases

| Database | Status | Spatial Support | Notes |
|----------|--------|-----------------|-------|
| **PostgreSQL** (+ PostGIS) | ✅ Fully Supported | ✅ Excellent | Recommended. Full PostGIS support for spatial operations |
| **SQL Server** | ✅ Fully Supported | ✅ Good | Spatial types and functions available since SQL Server 2008 |
| **MySQL 8.0+** | ⚠️ Configurable | ⚠️ Limited | Spatial extensions available but less mature than PostGIS |
| **Oracle** | ⚠️ Configurable | ✅ Excellent | Oracle Spatial provides enterprise-grade geo capabilities |
| **SQLite** (+ SpatiaLite) | ⚠️ Configurable | ✅ Good | Suitable for development/testing, not recommended for production |
| **MariaDB** | ⚠️ Configurable | ⚠️ Limited | Basic geometry types, limited spatial functions |

### Database-Specific Considerations

Each database requires its own SQL query configuration due to syntax differences:

**1. Pagination Syntax:**
- **PostgreSQL/MySQL:** `LIMIT @limit`
- **SQL Server:** `TOP (@limit)` or `OFFSET 0 ROWS FETCH NEXT @limit ROWS ONLY`
- **Oracle:** `FETCH FIRST :limit ROWS ONLY`

**2. Parameter Prefix:**
- Configure `ParameterPrefix` in appsettings.json to match your database
- **PostgreSQL/SQL Server/MySQL:** `"ParameterPrefix": "@"` (default)
- **Oracle:** `"ParameterPrefix": ":"`
- Framework uses this prefix internally for all parameter mapping

**3. Type Casting:**
- **PostgreSQL:** `column::type` (e.g., `id::text`)
- **SQL Server:** `CAST(column AS type)` or `CONVERT(type, column)`
- **Oracle:** `CAST(column AS type)`

**4. Case Sensitivity:**
- **PostgreSQL:** Quoted identifiers are case-sensitive (`"MyTable"` ≠ `"mytable"`)
- **SQL Server:** Usually case-insensitive (depends on collation)

### Spatial Function Configuration

Spatial operations (e.g., `geo.distance`, `geo.intersects`) are configured per database in the `FunctionMapping` section:

**PostgreSQL/PostGIS example:**
```json
{
  "ODataFunctionName": "geo.distance",
  "SqlFunctionFormat": "ST_Distance(ST_Transform({0}::geometry, @srid), ST_GeomFromText({1}, 4258))",
  "ExpectedArgumentTypes": ["geography", "string"],
  "ReturnType": "Edm.Double"
}
```

**SQL Server example:**
```json
{
  "ODataFunctionName": "geo.distance",
  "SqlFunctionFormat": "geography::STGeomFromWKB({0}, 4258).STDistance(geography::STGeomFromText({1}, 4258))",
  "ExpectedArgumentTypes": ["geography", "string"],
  "ReturnType": "Edm.Double"
}
```

See the [DdApiV3Template Configuration](DigitaleDelta.DdApiV3Template/README.md) for complete examples and database configuration details.

---

Need more examples or want to see a minimal setup?
See [`examples/`](examples/) or consult the CONTRIBUTING.md.

