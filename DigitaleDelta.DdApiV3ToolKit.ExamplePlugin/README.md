# DigitaleDelta.DdApiV3ToolKit Voorbeeld Plugin

Dit is een voorbeeld plugin project dat laat zien hoe je custom autorisatie en request logging implementeert voor de DigitaleDelta API v3 ToolKit.

## Wat Deze Plugin Bevat

1. **ExampleAuthorizationHandler** - Toont hoe je `IAuthorization` implementeert
2. **ExampleRequestLogger** - Toont hoe je `IRequestLogger` implementeert

## Hoe Deze Plugin Te Gebruiken

### Stap 1: Pas de Implementaties Aan

Wijzig `ExampleAuthorizationHandler.cs` en `ExampleRequestLogger.cs` om de specifieke vereisten van jouw organisatie te implementeren:

- **Autorisatie**: Integreer met je authenticatiesysteem (AD, OAuth, database, etc.)
- **Request Logging**: Stuur logs naar je gewenste bestemming (database, bestand, monitoring service, etc.)

### Stap 2: Bouw de Plugin

```bash
dotnet build -c Release
```

Dit maakt een DLL aan in `bin/Release/net10.0/`

### Stap 3: Plaats de plugin

1. Kopieer de gecompileerde DLL naar de `Plugins` directory van de ToolKit:
   ```
   JouwToolKitDeployment/
   ├── Plugins/
   │   └── DigitaleDelta.DdApiV3ToolKit.ExamplePlugin.dll
   ├── ConfigurationFiles/
   │   └── appsettings.json
   └── DigitaleDelta.DdApiV3ToolKit.exe
   ```

2. Update `ConfigurationFiles/appsettings.json`:
   ```json
   {
     "PluginSettings": {
       "PluginDirectory": "Plugins",
       "AuthorizationHandler": "DigitaleDelta.DdApiV3ToolKit.ExamplePlugin.ExampleAuthorizationHandler, DigitaleDelta.DdApiV3ToolKit.ExamplePlugin",
       "RequestLogger": "DigitaleDelta.DdApiV3ToolKit.ExamplePlugin.ExampleRequestLogger, DigitaleDelta.DdApiV3ToolKit.ExamplePlugin"
     }
   }
   ```

### Stap 4: Start de ToolKit

De ToolKit zal automatisch je plugin ontdekken en laden bij het opstarten.

## Plugin Discovery

Het plugin systeem werkt op twee manieren:

1. **Expliciete Configuratie**: Specificeer de 'fully qualified type name' in `appsettings.json`
2. **Auto-Discovery**: Plaats een DLL in de `Plugins` folder - het systeem zal scannen en implementaties automatisch vinden

## Dependency Injection

Je plugin classes kunnen constructor injection gebruiken om toegang te krijgen tot services die geregistreerd zijn in de DI-container van de ToolKit, zoals:

- `IConfiguration` - Applicatie configuratie
- `ILogger<T>` - Logging service
- Elke andere service die geregistreerd is in de ToolKit

## Voorbeeld: Organisatie-Gebaseerde Autorisatie

```csharp
public Task<(bool authorised, string access)> TryAuthorizeAsync(ClaimsPrincipal? claimsPrincipal)
{
    var organizationId = claimsPrincipal?.FindFirst("org_id")?.Value;

    if (string.IsNullOrEmpty(organizationId))
    {
        // Weiger toegang
        return Task.FromResult((false, "1 = 0"));
    }

    // Filter data op organisatie
    return Task.FromResult((true, $"organization_id = {organizationId}"));
}
```

___Let op: er komt een SQL-segment terug uit deze functie. Dit deel wordt gecombineerd met de query om de data op te halen. Het vergelijkingselement moet dus voorkomen in de selectie van de opgebouwde SQL-query.___

## Voorbeeld: Database Request Logging

```csharp
public void LogRequest(string url, ClaimsPrincipal? claimsPrincipal, string requestId, DateTime requestStart)
{
    var userId = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    using var connection = new SqlConnection(_connectionString);
    connection.Execute(
        "INSERT INTO RequestLog (RequestId, UserId, Url, RequestStart) VALUES (@RequestId, @UserId, @Url, @RequestStart)",
        new { RequestId = requestId, UserId = userId, Url = url, RequestStart = requestStart }
    );
}
```

## Licentie

Copyright (c) 2025 - EcoSys
Licensed under the MIT License.
