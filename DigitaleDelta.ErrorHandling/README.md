# DigitaleDelta.ErrorHandling

Helpers en middleware voor **gestandaardiseerde OData-foutafhandeling** in .NET API’s.

Dit component zorgt voor consistente OData-fout-responses (volgens specificatie én Nederlandse API Designrules), met centrale registratie als middleware of exception-filter in ASP.NET Core. 
Context-specifieke informatie, zoals tracing- en session-informatie, kunnen worden toegevoegd aan de fout-response via uitbreidingen.

---

## Belangrijkste features

- **OData-compliant fout-responses** met uitbreidingsmogelijkheden (o.a. correlationId, details)
- **Middleware en ExceptionFilter** voor centrale foutafhandeling
- Makkelijk te integreren in bestaande ASP.NET Core applicaties
- Uitbreidbaar en volledig onafhankelijk van Microsoft OData-libraries
- Aanpasbaar voor implementatie van domeinspecifieke error-codes en -meldingen

---

## Gebruik

### 1. Registratie als middleware

In `Program.cs`:

```csharp 
app.UseODataErrorHandling();
```

Registreer deze idealiter direct na authenticatie/autorisatie, vóór de controllers.

### 2. Toevoegen van uitbreidingen aan error responses

Je kunt extra informatie zoals een **sessionId** of **correlationId** toevoegen aan error responses:

```csharp 
var result = ODataErrorResponseHelper.Create(serverError, code, status, target, details, innerError, type, instance);
```

### 3. Eigen ODataValidationException gebruiken

Gooi bij validatieproblemen altijd een `ODataValidationException`. 
Zo wordt deze correct afgehandeld via de middleware/filter:

```csharp 
throw new ODataValidationException("Filter-syntax onjuist.", code: "InvalidFilter");
```

---

## Voorbeeldresponse

```json
{ 
  "error": { 
    "code": "ValidationError", 
    "message": "Het meegegeven filter is niet geldig.", 
    "status": 400, 
    "instance": "urn:traceid:abcd-1234"
  }
}
```
---

## Meewerken

Pull requests, verbeteringen en feedback zijn welkom!  
Zie CONTRIBUTING.md voor richtlijnen.

---

## Licentie

MIT License. Zie het LICENSE-bestand voor details.

---