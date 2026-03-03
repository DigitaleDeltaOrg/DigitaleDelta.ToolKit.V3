# DigitaleDelta.RateLimiting

Flexible and configurable rate limiting middleware (action filter) for modern .NET APIs.  
DigitaleDelta.RateLimiting allows you to easily add rate limiting per user, client, API key, or IP—using a simple configuration, extensible logic, and robust ASP.NET Core integration.

---

## Features

- **Flexible rate limiting:**  
  Limit requests per user, IP address, API key, certificate, or custom identity logic.
- **Concurrent request limiting:**  
  Enforce both time-window and concurrent request limits per identity.
- **Configurable:**  
  Configure limits per controller (action) via `appsettings.json` or programmatically.
- **Works with proxies:**  
  Supports request identification using X-Forwarded-For and other headers.
- **Global or attribute-based:**  
  Apply globally to all controllers/actions, or locally using attributes.
- **Dependency Injection & testable:**  
  Fully integrates with ASP.NET Core dependency injection and configuration.

---

## Installation

``` bash
  dotnet add package DigitaleDelta.RateLimiting
```
---

## Usage

### Basic configuration

Add your rate limiting options to `appsettings.json`:


### Register in Program.cs

**Best practice:** use the extension method to register DigitaleDelta.RateLimiting globally for all controllers:


```csharp 
using DigitaleDelta.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddDigitaleDeltaRateLimiting(builder.Configuration);
var app = builder.Build(); 
app.MapControllers(); 
app.Run();
```

Or add the filter per controller/action using `[ServiceFilter]` or `[TypeFilter]` if you want more control.

---

## How it works

- For each incoming request, the filter identifies the client by user, API key, certificate, or (if no other identity is available) by IP address.
- Maintains an in-memory log of requests per identity.
- If the configured limit or concurrent limit is exceeded, the filter sets an HTTP error status (`429` or `503`) and a descriptive response.
- Rate limit information is exposed via HTTP response headers:
    - `RateLimit-Limit`
    - `RateLimit-Remaining`
    - `RateLimit-Reset`

---

## Example: Custom Rate Limiting Logic

You can override the way identity is extracted (e.g., to combine JWT, session, API key, certificate, forwarded headers, etc.) by copying or extending the provided filter.

---

## Configuration Reference

**RateLimitOptions:**

| Property                   | Type   | Description                                          |
|----------------------------|--------|------------------------------------------------------|
| Limit                      | int    | Maximum requests per time window                     |
| Unit                       | string | Time unit: "s" (seconds), "m" (minutes), "h" (hours) |
| NumberOfConcurrentRequests | int    | Number of allowed concurrent requests                |

---

## Testing

This project uses xUnit for unit testing.

### Running tests

``` bash
  dotnet test
```

---

## Contributing

Pull requests and feedback are welcome.  
For guidelines, please see CONTRIBUTING.md.

---

## License

MIT License. See the LICENSE file for full license information.

---