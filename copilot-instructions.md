# GitHub Copilot Instructions

## Code Style Guidelines

### Zero Warnings Policy
- **Always aim for zero warnings and messages on every build**
- Address all compiler warnings before considering code complete
- Use `#pragma warning disable` only in exceptional cases with clear justification
- Enable "Treat warnings as errors" where appropriate

### Modern C# Practices (.NET 11.0)

#### Primary Constructors
- **Use primary constructors where possible** for cleaner, more concise code:
```csharp
// Preferred
public class HaloClient(HaloClientOptions options) : IHaloClient
{
    public string Account => options.HaloAccount;
}

// Instead of
public class HaloClient : IHaloClient
{
    private readonly HaloClientOptions _options;
    
    public HaloClient(HaloClientOptions options)
    {
        _options = options;
    }
    
    public string Account => _options.HaloAccount;
}
```

#### Collection Initialization
- **Use collection expressions `[]` instead of `new List<T>()`**:
```csharp
// Preferred
var items = [item1, item2, item3];
var emptyList = <string>[];

// Instead of
var items = new List<string> { item1, item2, item3 };
var emptyList = new List<string>();
```

#### Required Properties
- Use `required` keyword for mandatory properties:
```csharp
public class HaloClientOptions
{
    public required string HaloAccount { get; init; }
    public required string HaloClientId { get; init; }
}
```

#### File-Scoped Namespaces
- Always use file-scoped namespaces:
```csharp
// Preferred
namespace HaloPsa.Api;

public class HaloClient
{
}

// Instead of
namespace HaloPsa.Api
{
    public class HaloClient
    {
    }
}
```

#### String Interpolation
- Use string interpolation over concatenation:
```csharp
// Preferred
var message = $"Error in {methodName}: {errorDetails}";

// Instead of
var message = "Error in " + methodName + ": " + errorDetails;
```

#### Pattern Matching
- Use modern pattern matching features:
```csharp
// Preferred
var result = value switch
{
    null => "null value",
    string s when s.Length > 0 => $"String: {s}",
    _ => "other"
};

// Use is patterns for type checks
if (obj is HaloClient client && client.IsValid)
{
    // Process client
}
```

#### Null Handling
- Use null-conditional operators and null-coalescing:
```csharp
// Preferred
var result = client?.GetData()?.FirstOrDefault() ?? defaultValue;

// Use ArgumentNullException.ThrowIfNull
ArgumentNullException.ThrowIfNull(parameter);
```

#### Expression-Bodied Members
- Use expression-bodied members for simple operations:
```csharp
// Preferred
public string FullName => $"{FirstName} {LastName}";
public void LogError(string message) => _logger.LogError(message);
```

#### Records
- Use records for immutable data structures:
```csharp
public record HaloApiResponse(string Data, int StatusCode, DateTime Timestamp);
```

### Code Organization

#### Using Statements
- Place using statements outside namespace (file-scoped)
- Group and sort using statements
- Remove unused using statements

#### Access Modifiers
- Always be explicit about access modifiers
- Use `internal` for implementation details not meant for public API

#### Naming Conventions
- Use PascalCase for public members
- Use camelCase with underscore prefix for private fields: `_fieldName`
- Use meaningful, descriptive names

### Testing Standards
- Use AwesomeAssertions for fluent test assertions
- Follow AAA pattern (Arrange, Act, Assert)
- Use descriptive test method names that explain the scenario

### EditorConfig Compliance
- Follow the .editorconfig settings in the workspace
- Use tabs for indentation as configured
- Maintain consistent formatting across all files

### Performance Considerations
- Use `ConfigureAwait(false)` for library code
- Prefer `StringBuilder` for multiple string concatenations
- Use `span` and `memory` types where appropriate for performance-critical code

### Documentation
- Use XML documentation comments for public APIs
- Include `<summary>`, `<param>`, and `<returns>` tags
- Document any non-obvious behavior or assumptions

### Error Handling
- Use specific exception types (FormatException, ArgumentException, etc.)
- Provide meaningful error messages
- Use structured logging where applicable

## Project-Specific Guidelines

### Halo PSA API Library Structure

#### Project Organization
- **Main Library**: `HaloPsa.Api` (targets .NET 11.0)
  - Core client classes and interfaces
  - API models and DTOs (to be generated from OpenAPI spec)
  - Authentication and configuration
  
- **Test Project**: `HaloPsa.Api.Test` (targets .NET 11.0)
  - Unit tests with AwesomeAssertions
  - Integration tests using Microsoft Testing Platform
  - Uses User Secrets for sensitive configuration

#### Authentication & Configuration
```csharp
// HaloClientOptions pattern - always use required properties
public class HaloClientOptions
{
    public required string HaloAccount { get; init; }
    public required string HaloClientId { get; init; }     // GUID format
    public required string HaloClientSecret { get; init; } // Two concatenated GUIDs
}
```

#### Validation Patterns
- Use `Regex` source generators for input validation:
```csharp
[GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled)]
private static partial Regex GetGuidRegex();
```

#### Interface Design
- All public client functionality should be exposed through interfaces
- Use `IHaloClient` for main client operations
- Consider creating specialized interfaces for different API areas (e.g., `ITicketsApi`, `IUsersApi`)

#### OpenAPI Integration
- The project includes a comprehensive `swagger.json` specification
- Generate API models and clients from this specification
- API supports extensive filtering, pagination, and field selection
- Common patterns:
  - List endpoints: Support filtering, sorting, pagination
  - Single endpoints: Support `includedetails` parameter
  - POST/PUT: Accept arrays for batch operations

#### Test Configuration
- Integration tests use User Secrets (ID: `4b5a486e-641f-4a44-b583-4419dbb564a9`)
- Test configuration structure:
```json
{
  "HaloApi": {
    "HaloAccount": "your-account",
    "HaloClientId": "guid-here",
    "HaloClientSecret": "two-guids-concatenated"
  }
}
```

#### Testing Framework Setup
- Uses Microsoft Testing Platform (not traditional xUnit runner)
- Code coverage with Microsoft.Testing.Extensions.CodeCoverage
- Global usings for `Xunit` and `Microsoft.Extensions.Logging`

#### API Endpoint Patterns
Based on the OpenAPI spec, follow these patterns:

1. **Resource Collections**: `/Resource` (GET for list, POST for create/update)
2. **Single Resources**: `/Resource/{id}` (GET for single, DELETE for removal)
3. **Special Operations**: `/Resource/Action` for specific operations
4. **Common Query Parameters**:
   - `count` - Number of results to return
   - `page_no`, `page_size` - Pagination
   - `order`, `orderdesc` - Sorting
   - `includedetails` - Extended information
   - `search` - Text search

#### Error Handling Patterns
```csharp
// Validation in client options
internal void Validate()
{
    if (string.IsNullOrWhiteSpace(HaloAccount))
        throw new ArgumentException("HaloAccount cannot be null or empty.", nameof(HaloAccount));
    
    if (!_guidRegex.IsMatch(HaloClientId))
        throw new FormatException("HaloClientId must be a valid GUID format.");
}
```

#### Logging Integration
- Use structured logging with Microsoft.Extensions.Logging
- Log important operations and errors
- Use appropriate log levels (Information for normal operations, Warning for issues)

### Build and Packaging
- Uses Nerdbank.GitVersioning for semantic versioning (current: `2.196-alpha`)
- Source Link enabled for debugging support
- Package generation enabled with comprehensive metadata
- Zero warnings policy enforced at build time
- XML documentation generation required for all public APIs

### Development Tools
- Microsoft Testing Platform for test execution
- AwesomeAssertions for fluent test syntax
- User Secrets for local development configuration
- Git versioning with semantic release tags

## Ongoing Development Guidelines

### Implementation Plan Reference
- **📋 Implementation Plan**: See `Specification/ImplementationPlan.md` for the complete phased development approach
- **🎯 Current Focus**: Check the "Current Phase" section in the implementation plan
- **📊 Progress Tracking**: Update the implementation plan as each phase/milestone is completed

### Development Workflow
1. **Always reference the implementation plan** before starting new work
2. **Update progress markers** in the implementation plan as work is completed
3. **Follow the phased approach** - don't jump ahead to later phases
4. **Run integration tests** after each significant change
5. **Maintain zero warnings** policy throughout development

### Refit Integration Guidelines
- Use Refit for HTTP client generation from OpenAPI spec
- Follow Meraki.Api patterns for HTTP handlers and interceptors
- Implement custom `DelegatingHandler` for logging and retry logic
- Structure APIs by functional groups (e.g., `IPsaApi`, `IServiceDeskApi`)

### API Client Structure
```csharp
// Target API structure
var client = new HaloClient(options);
await client.Psa.Tickets.GetAllAsync(filter, cancellationToken);
await client.ServiceDesk.Assets.GetByIdAsync(id, cancellationToken);
```

### Integration Testing Approach
- Use full administrator credentials in sandbox environment
- Always restore system state after tests (create/cleanup pattern)
- Test each API group thoroughly before moving to next phase
- Include both positive and negative test cases

### Questions and Clarifications
- **When stuck**: Add questions to the "Clarifying Questions" section in the implementation plan
- **Before major decisions**: Consult the implementation plan and add questions if needed
- **During reviews**: Update completed sections and mark progress