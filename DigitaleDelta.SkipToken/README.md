# DigitaleDelta.SkipToken

A helper for dealing with OData skip tokens.
Uses HMAC to avoid client-side tempering.
A timestamp is added to the skip token to prevent replay attacks.

---
## Features

- Creates a $skipToken based on an existing Url and an id.
- Extracts and validates a $skipToken from a Url.


The creator of SkipTokenHelper requires one or two parameters:

- The secret used to sign the skip token.
- An optional algorithm to use for signing. Defaults to "SHA256" for HMAC/SHA256. Alternative values are "SHA512" for HMAC/SHA512.

---
## Tip

The secret should be at least 16 characters long and randomly generated and not shared or hard-coded.

---
## Usage (Extract)

```
      var helper = new SkipTokenHelper("your-secret-here");
      var valid = helper.ExtractFromUrl(url, 10, out SkipToken? skipToken);
```

---
## Usage (Create)

```
      var helper = new SkipTokenHelper("your-secret-here", "SHA512");
      var valid = SkipToken.TryConstructFromUrl(baseUrl, lastId, out string? tokenString, out string? error);
```

---
## Testing

This project uses xUnit for unit and integration tests. 

---
### Running Tests

```bash
  dotnet test
```

---
## Contributing

Pull requests, improvements, and feedback are welcome!  
For guidelines, please see CONTRIBUTING.md.

---