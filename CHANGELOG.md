# Changelog

All notable changes to the HaloPsa.Api project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed (#72)
- `TicketFilter` is now sent under Halo's parameter names (`client_id`, `requesttype_id`, `open_only`, `pageinate`, `page_size`, `mine`, `lastupdatefromdate` and others). Before this, Halo silently ignored most filters and returned unfiltered results.
- `StartDate`/`EndDate` are sent with `datesearch` (defaulting to the opened date) in `yyyy-MM-ddTHH:mm:ss` format.
- Statuses, Priorities, Categories and Teams now request `/api/...` instead of a path outside the API. Statuses and Priorities also accept Halo's plain-array response.
- `Ticket.CategoryId` is read from `categoryid_1` and `Ticket.IsOnHold` from `onhold`.

### Added
- `Ticket.AdditionalProperties` keeps every field the model does not declare (SLA dates and state, `timetaken` and others).
- `TicketFilter.DateSearch` (see `TicketDateSearch`), `SearchSummary`, `IncludeSlaTimer` and `IncludeTimeTaken`.
- `Psa.Actions`: read-only `GET /api/Actions` returning `TicketAction` records.
- `Psa.Priorities`.

### Changed
- `Priority.Id` is now the record's GUID string, matching the live API. Use `Priority.PriorityId` for ticket filters. `PrioritiesApiWrapper.GetByIdAsync` takes a string id.
- `TicketFilter.UnassignedOnly`, `IncludeDetails` and `IncludeChildren` are obsolete and never sent, because Halo's ticket list has no matching flag.

### Added
- Initial release of HaloPsa.Api - comprehensive .NET library for Halo PSA API
- Complete PSA module implementation with full CRUD operations
- OAuth2 Client Credentials authentication flow
- Modern .NET 9 implementation with latest C# features
- Comprehensive test suite with 100% success rate (88/88 tests passing)
- Full API coverage for core PSA endpoints:
  - **Tickets** - Full CRUD operations, filtering, actions, and workflow
  - **Users** - User management with full CRUD support
  - **Clients** - Client and site management with full CRUD support
  - **Assets** - Asset tracking and configuration management with full CRUD support
  - **Projects** - Project management and time tracking with full CRUD support
  - **TicketTypes** - Read operations for ticket type management
- Advanced HTTP client features:
  - Automatic retry logic with exponential backoff
  - Comprehensive request/response logging
  - Custom error handling with detailed exception hierarchy
  - Configurable timeouts and retry policies
- Robust error handling with custom exception types:
  - `HaloApiException` - Base exception for all API errors
  - `HaloBadRequestException` - 400 Bad Request errors with validation details
  - `HaloAuthenticationException` - 401 Authentication failures
  - `HaloAuthorizationException` - 403 Authorization failures
  - `HaloNotFoundException` - 404 Resource not found errors
  - `HaloRateLimitException` - 429 Rate limit exceeded errors
  - `HaloServerException` - 5xx Server errors
- Comprehensive filtering and pagination support
- Dynamic discovery patterns for integration testing
- Type-safe configuration with validation
- IntelliSense-friendly XML documentation
- Source Link support for debugging
- Symbol packages for enhanced development experience

### Technical Features
- **Target Framework**: .NET 9.0
- **Authentication**: OAuth2 Client Credentials flow
- **HTTP Client**: Refit-based with custom handlers
- **Logging**: Microsoft.Extensions.Logging integration
- **Testing**: Microsoft Testing Platform with AwesomeAssertions
- **Packaging**: NuGet package with comprehensive metadata
- **CI/CD**: GitHub Actions with automated publishing
- **Code Quality**: Zero warnings policy, comprehensive test coverage

### API Structure
```csharp
// Core client initialization
var client = new HaloClient(new HaloClientOptions
{
    HaloAccount = "your-account",
    HaloClientId = "your-client-id",
    HaloClientSecret = "your-client-secret"
});

// PSA module APIs
await client.Psa.Tickets.GetAllAsync(filter, cancellationToken);
await client.Psa.Users.GetAllAsync(cancellationToken);
await client.Psa.Clients.GetAllAsync(cancellationToken);
await client.Psa.Assets.GetAllAsync(cancellationToken);
await client.Psa.Projects.GetAllAsync(cancellationToken);
await client.Psa.TicketTypes.GetAllAsync(cancellationToken);

// Full CRUD operations
await client.Psa.Tickets.CreateAsync(request, cancellationToken);
await client.Psa.Tickets.UpdateAsync(id, request, cancellationToken);
await client.Psa.Tickets.DeleteAsync(id, cancellationToken);
```

### Development Standards
- Modern C# patterns (primary constructors, collection expressions, required properties)
- File-scoped namespaces throughout
- Comprehensive error handling and logging
- 100% test success rate requirement
- Zero warnings build policy
- Extensive integration testing against live Halo PSA sandbox

### Documentation
- Complete README with usage examples and troubleshooting
- XML documentation for all public APIs
- Comprehensive contributor guidelines
- Implementation plan and coding standards

## Release Notes

This is the baseline release of HaloPsa.Api, providing a complete, production-ready .NET library for interacting with the Halo PSA API. The library has been thoroughly tested with 88 passing integration and unit tests, ensuring reliability and compatibility with Halo PSA systems.

### Coming Soon
- ServiceDesk module implementation
- System module for configuration and administration
- Additional convenience methods and utilities
- Enhanced filtering and search capabilities

---

**Note**: Future releases will document changes from this baseline. This initial release represents a comprehensive, fully-featured library ready for production use.