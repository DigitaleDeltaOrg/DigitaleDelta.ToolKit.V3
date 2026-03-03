# Contributing to DigitaleDelta OData Library

Thank you for your interest in contributing to the DigitaleDelta OData Library! This document provides guidelines and information for contributors.

## Project scope and focus

This repository contains several reusable .NET libraries (such as the OData translator/writer and query service) and the **DigitaleDelta DdApiV3 Toolkit**, which together provide a configurable, read-only DD API V3 implementation.

The main focus is:
- A **configuration-driven reference implementation** of DD API V3 (the Toolkit);
- Generic but DD-API-driven components for OData translation, query handling and related functionality;
- Support for organisations (e.g. water authorities) and their vendors that want to host DD API V3 in their own infrastructure.

The project is **not** intended to cover every possible generic OData/SQL scenario or all infrastructure variants. Design and feature decisions are primarily driven by DD API V3 use cases.

## Ways to contribute

You can contribute in several ways:
- Reporting **bugs** and problems;
- Improving **documentation** (READMEs, examples, comments);
- Providing **small bugfixes or improvements** to existing functionality;
- Adding or improving **tests** for existing features;
- Contributing **examples**, such as plugin implementations (e.g. custom `IRequestLogger` or `IAuthorization`), configuration samples, or deployment snippets but _without_ sensitive information.

For larger changes (new features, public API changes, refactorings), please open an **issue for discussion first** before investing in a pull request.

## Roles and responsibilities

- **Maintainers** focus on:
  - The core libraries (including nuget packages) and Toolkit architecture;
  - Keeping the DD API V3 implementation stable and backwards compatible where possible;
  - Reviewing and merging contributions.

- **Organisations and their vendors** are responsible for:
  - Hosting and deployment (on-prem, cloud, reverse proxy, containers, etc.);
  - Identity provider configuration (Keycloak, Azure AD/Entra ID, ...);
  - Infrastructure-specific concerns such as TLS termination, firewalls, monitoring, and secrets management.

Contributions that improve configurability, observability, and robustness (without coupling to a specific vendor/platform) are especially welcome.

### Registering DD API V3 implementations (optional)

To help the community understand **where and how** the DigitaleDelta DD API V3 Toolkit is used, we maintain (or plan to maintain) a simple overview of implementations.
If you deploy the Toolkit in a pilot or production-like environment, we kindly ask you to share some high-level information about your setup:

- **Organisation name** (e.g. water authority, consultancy, vendor);
- **Type of environment** (pilot, test, acceptance, production);
- **Accessibility** (public internet, restricted network, internal only);
- **Water management segment** (e.g. surface water, groundwater, water quality, hydrology, flood risk, etc.);
- **Country/region**;
- **Base URL(s)** of the DD API V3 endpoint(s), if they are publicly reachable;
- Optionally: a contact point (team, role or generic e-mail address).

This information can be used to:
- Build a non-exhaustive **overview of implementations** for the community;
- Improve **discovery** of available DD API V3 endpoints;
- Gain insight into which types of organisations and environments are using the Toolkit.

If you want to register your implementation, please:
- Open a short **GitHub issue** with the label `implementation-info` that contains the information above, or
- (Once available) add an entry to the central `IMPLEMENTATIONS.md` overview via a pull request.

Please **do not** include any sensitive information (no internal IP addresses, credentials, or detailed descriptions of internal security architecture).

## Opening issues

Before opening a new issue:
1. Search existing issues to see if your problem or idea has already been reported.
2. Clearly indicate whether it is a:
   - **Bug report** (with reproducible steps),
   - **Feature request / enhancement**, or
   - **Question / discussion**.

Useful information for a bug report:
- Which component(s) you are using (Toolkit, ODataTranslator, QueryService, etc.);
- .NET version and runtime/OS;
- Minimal steps to reproduce the problem;
- Expected vs. actual behaviour;
- Relevant configuration snippets (without secrets such as passwords or tokens).

## Pull requests

When opening a pull request:
- Keep the PR **focused on a single topic** (one feature, fix, or documentation change);
- Make sure the code **builds** and all relevant tests pass locally;
- Add or update **tests** where appropriate;
- Update documentation (README, XML docs, examples) if you change public APIs or configuration shapes;
- Avoid breaking changes unless discussed and accepted beforehand.

The rest of this document describes the coding style, testing principles and architectural guidelines used in this repository.

## Code of Conduct

This project follows standard open-source community guidelines. Please be respectful and constructive in all interactions.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a branch** for your changes: `git checkout -b feature/your-feature-name`
4. **Make your changes** following the code style guidelines below
5. **Test your changes** thoroughly
6. **Commit your changes** with clear, descriptive commit messages
7. **Push to your fork** and submit a pull request

## Code Style Guidelines

This project follows modern C# conventions and uses an `.editorconfig` file to enforce consistent formatting. Most IDEs (Visual Studio, Rider, VS Code with C# extension) will automatically apply these settings.

### Key Conventions

#### Naming Conventions

- **Classes, Interfaces, Methods, Properties**: `PascalCase`
  ```csharp
  public class EntityType { }
  public interface IAuthorization { }
  public string PropertyName { get; set; }
  ```

- **Interfaces**: Must start with `I`
  ```csharp
  public interface IRequestLogger { }
  ```

- **Private fields**: `_camelCase` (underscore prefix)
  ```csharp
  private readonly string _connectionString;
  ```

- **Constants**: `camelCase` (Note: This is a project-specific convention)
  ```csharp
  public const string entitySetNameIsRequired = "EntitySet name is required.";
  ```

- **Method parameters and local variables**: `camelCase`
  ```csharp
  public void ProcessData(string inputValue, int recordCount)
  {
      var result = DoWork();
  }
  ```

#### Formatting

- **Indentation**: 4 spaces (no tabs)
- **Brace style**: Allman style (braces on new lines)
  ```csharp
  if (condition)
  {
      // code
  }
  ```

- **Line length**: Keep lines under 120-140 characters when practical
- **Spacing**:
  - Space after keywords in control flow statements: `if (condition)`
  - Spaces around binary operators: `var result = a + b;`
  - No space between method name and opening parenthesis: `Method()`

#### Modern C# Features

This project uses C# 12 features. Please use them where appropriate:

- **File-scoped namespaces**:
  ```csharp
  namespace DigitaleDelta.Contracts;

  public class MyClass { }
  ```

- **Primary constructors**:
  ```csharp
  public class Logger(string logPath)
  {
      private readonly string _logPath = logPath;
  }
  ```

- **Collection expressions**:
  ```csharp
  public List<string> Items { get; set; } = [];
  private readonly Dictionary<string, object> _cache = [];
  ```

- **Init-only properties**:
  ```csharp
  public required string Name { get; init; }
  ```

- **Record types** for DTOs:
  ```csharp
  public record SqlResult(string Sql, IReadOnlyDictionary<string, object> Parameters);
  ```

- **Switch expressions**:
  ```csharp
  return type switch
  {
      "string" => EdmType.EdmString,
      "int" => EdmType.EdmInt32,
      _ => EdmType.EdmUnknown
  };
  ```

- **Nullable reference types**: Always use nullable annotations
  ```csharp
  public string? OptionalValue { get; set; }
  public string RequiredValue { get; set; } = string.Empty;
  ```

#### Design Patterns

- **Try-pattern**: Use for operations that can fail
  ```csharp
  public static bool TryParse(string input, out Result? result, out string? error)
  {
      // implementation
  }
  ```

- **Async/await**: Always use `ConfigureAwait(false)` in library code
  ```csharp
  await ProcessAsync().ConfigureAwait(false);
  ```

- **Dependency Injection**: Use constructor injection with readonly fields
  ```csharp
  public class Service(ILogger logger)
  {
      private readonly ILogger _logger = logger;
  }
  ```

#### Documentation

- **XML documentation**: All public APIs must have XML documentation
  ```csharp
  /// <summary>
  /// Parses an OData filter expression.
  /// </summary>
  /// <param name="query">The OData filter query string.</param>
  /// <param name="filter">The parsed filter result.</param>
  /// <param name="error">Error message if parsing fails.</param>
  /// <returns>True if parsing succeeded, false otherwise.</returns>
  public static bool TryParse(string query, out ODataFilter? filter, out string? error)
  ```

- **Comments**: Use comments to explain "why", not "what". The code should be self-documenting for "what".
  ```csharp
  // Bad
  // Increment counter
  counter++;

  // Good
  // Increment to account for zero-based indexing in the API response
  counter++;
  ```

#### File Organization

- **Using directives**: Place at the top of the file, sorted (System namespaces first)
- **File header**: Include copyright notice
  ```csharp
  // Copyright (c) 2025 - EcoSys
  // Licensed under the MIT License. See LICENSE file in the project root for full license information.
  ```

- **One type per file**: Each class, interface, record, or enum should be in its own file
- **File naming**: Match the type name (e.g., `EntityType.cs` for `class EntityType`)

## Testing

- Write unit tests for all new functionality
- Ensure all existing tests pass before submitting a pull request
- Aim for high code coverage, especially for public APIs
- Use descriptive test names that explain what is being tested

```csharp
[Fact]
public void TryParse_WithValidFilter_ReturnsTrue()
{
    // Arrange
    var query = "name eq 'test'";

    // Act
    var result = ODataFilter.TryParse(query, out var filter, out var error);

    // Assert
    Assert.True(result);
    Assert.NotNull(filter);
    Assert.Null(error);
}
```

## Pull Request Guidelines

- **Keep PRs focused**: One feature or fix per pull request
- **Write clear descriptions**: Explain what changes you made and why
- **Reference issues**: Link to any related GitHub issues
- **Update documentation**: If you change public APIs, update the relevant documentation
- **Ensure CI passes**: All tests and code quality checks must pass

### Commit Messages

Write clear, descriptive commit messages:

```
Add support for spatial filtering in OData queries

- Implement distance() function for geographic queries
- Add support for WKT geometry parsing
- Update documentation with spatial filter examples

Fixes #123
```

## Architecture Principles

This project follows specific architectural principles:

1. **Configuration-driven**: Prefer configuration over code where possible
2. **Performance-first**: Optimize for high-throughput scenarios with large datasets
3. **Stateless**: The API should be stateless and RESTful
4. **Type-safe**: Use strong typing and avoid reflection in hot paths
5. **Database-agnostic**: Support both PostgreSQL and SQL Server

Please keep these principles in mind when contributing.

## Performance Considerations

- **Avoid allocations in hot paths**: Use object pooling, `Span<T>`, or `stackalloc` where appropriate
- **Use `AggressiveInlining`** for small, frequently-called methods
- **Minimize reflection**: Pre-compute or cache reflection-based operations
- **Profile before optimizing**: Use benchmarks to validate performance improvements

## Deployment, Docker and Infrastructure-related Contributions

Because the DigitaleDelta stack is designed to be **hosted inside many different infrastructures**, contributions around deployment and operations should follow a few additional guidelines:

- **Keep examples generic**  
  Contributions are welcome for:
  - Example `Dockerfile`s or containerization patterns;
  - Generic reverse-proxy examples (nginx, IIS, etc.);
  - Health-check patterns (e.g. how to call `/health` from a load balancer or orchestrator);
  - Configuration and secrets patterns (environment variables, Azure Key Vault, Kubernetes/Docker secrets).

- **Avoid hard dependencies on a single platform**  
  Do not add code that ties the core libraries or the Toolkit directly to a specific cloud, orchestrator or product (e.g. Kubernetes, Azure-specific SDKs, AWS-specific SDKs, etc.).  
  Instead, prefer documenting **how** such platforms can be used around the Toolkit.

- **Secrets and Key Vault / secret stores**  
  The preferred pattern is:
  - The Toolkit reads configuration from `appsettings.json`, optional `appsettings.{Environment}.json` and environment variables;
  - Infrastructures like Azure Key Vault, HashiCorp Vault, or Kubernetes/Docker secrets inject values as environment variables or config files;
  - No direct coupling to a particular secrets product in the core Toolkit.

- **Identity providers (e.g. Keycloak)**  
  Contributions that improve JWT/OIDC configurability (issuer, audience, metadata endpoints, etc.) are welcome, as long as they:
  - remain **provider-agnostic** (work with Keycloak, Azure AD/Entra ID, and others);
  - do not introduce hard dependencies on a single identity provider.

If you are unsure whether a proposed deployment- or infrastructure-related change is in scope, please open an issue first to discuss your idea.

## Questions?

If you have questions or need help:

- Open an issue on GitHub for bug reports or feature requests
- Start a discussion for general questions or ideas

We appreciate your contributions!
