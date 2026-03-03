# DigitaleDelta.Authentication

A modular, configurable authentication middleware for modern .NET APIs.  
Supports various authentication methods (API Key, JWT (OpenID Connect/OAUTH2), mTLS, etc.) 
and always delivers an identifiable `ClaimsPrincipal`—ready for your own pipeline, 
logging, rate limiting, or authorization.

---

## Features

- **Pluggable authentication handlers:**  
  Use different handlers per endpoint or application: API keys, JWT/Bearer tokens, client certificates (mTLS), or even no authentication at all.
- **Always returns a `ClaimsPrincipal`:**  
  Each handler either builds an identity from request data or returns `null`.
- **Fully configurable:**  
  Set handlers, header names, and other settings from your configuration.
- **Separation of authentication and authorization:**  
  This module only establishes identity via ClaimPrinciple; all authorization logic remains up to you.
- **Perfect for logging, metrics, and rate limiting.**

---

## Installation

```bash
dotnet add package DigitaleDelta.Authentication
```

---

## Usage

### Register an authentication handler

```csharp
builder.Services.AddSingleton<IAuthenticationHandler>(new ApiKeyAuthenticationHandler("X-API-KEY"));
// Or use JwtAuthenticationHandler, MTLSAuthenticationHandler, ... or your own!

app.Use(async (context, next) =>
{
    var handler = context.RequestServices.GetRequiredService<IAuthenticationHandler>();
    var principal = await handler.AuthenticateAsync(context);

    if (principal != null)
        context.User = principal;

    await next();
});
```

### Example: ApiKeyAuthenticationHandler

```csharp
public class ApiKeyAuthenticationHandler : IAuthenticationHandler
{
    private readonly string _headerName;
    public ApiKeyAuthenticationHandler(string headerName)
    {
        _headerName = headerName;
    }

    public Task<ClaimsPrincipal?> AuthenticateAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_headerName, out var apiKey))
        {
            var claims = new[] { new Claim("api_key", apiKey!) };
            var identity = new ClaimsIdentity(claims, authenticationType: "APIKEY");
            return Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(identity));
        }
        return Task.FromResult<ClaimsPrincipal?>(null);
    }
}
```

---

## Supported authentication handlers

- **ApiKeyAuthenticationHandler:**  
  Extracts an API key from a configurable header and builds a minimal `ClaimsPrincipal`.
- **JwtAuthenticationHandler:**  
  Validates JWT/Bearer tokens and builds a `ClaimsPrincipal` from its claims.
- **MTLSAuthenticationHandler:**  
  Builds an identity from the client certificate’s information (subject, thumbprint, etc.).
- **NoAuthenticationHandler:**  
  Always returns an anonymous identity with a unique claim (such as session or random id), suitable for rate limiting.

---

## Extensibility

To implement your own handler:

```csharp
public class CustomTokenHandler : IAuthenticationHandler
{
    public Task<ClaimsPrincipal?> AuthenticateAsync(HttpContext context)
    {
        // Your authentication logic...
    }
}
```

---

## Testing

This project uses xUnit for unit testing.

### Running Tests

```bash
dotnet test
```

---

## Contributing

Pull requests, improvements, and feedback are welcome!  
For guidelines, please see CONTRIBUTING.md.

---
