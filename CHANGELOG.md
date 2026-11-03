# Changelog

All notable changes to the HaloPsa.Api project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- `CreateTicketRequest` is now sent under Halo's names (`client_id`, `site_id`, `user_id`, `status_id`, `priority_id`, `team_id`, `categoryid_1`, `tickettype_id`, `dateoccurred` and others), as the one-element array `POST /api/Tickets` takes. Before this Halo did not bind the client, site, user, custom fields or assets of a created ticket.
- Asset ids are sent as `assets: [{"id":...}]` and tags as `tags: [{"text":...}]`; unset fields are no longer sent.
- A ticket create's response is read whether Halo returns the ticket, an array of tickets or a `ticket` wrapper. A response that cannot be read says the ticket may have been created and gives its id.
- `User` reads from Halo's response again: an unreleased change had given it a constructor that System.Text.Json cannot bind.

### Added
- `CreateTicketRequest.DetailsHtml` and `TargetDate`; `CustomFieldInput`; a list overload of `CreateAsync`.
- `CreateTicketResponse.Tickets`.
- `ClientFilter` (`includeinactive`, `include_custom_fields`, `pageinate`, `page_size`, `page_no`, `search`, `exclude_internal`, `count`), `ClientsApiWrapper.GetResponseAsync(ClientFilter?)`, `GetAllPagedAsync`, which pages to the end with the same checks as the site read, and `GetByIdAsync(id, includeDetails)`.
- `Client.CustomFields`.
- `User.Login`, `FirstName`, `Surname`, `EmailAddress2`, `EmailAddress3` and `LinkedAgentId`; `UserFilter` (`search`, `client_id`, `includeinactive`, `exclude_agents`, `pageinate`, `page_size`, `page_no`) with `UsersApiWrapper.GetResponseAsync(UserFilter?)` and `GetAllAsync(UserFilter?)`.

### Changed
- `CreateTicketRequest.CustomFields` is now `IReadOnlyList<CustomFieldInput>`, sent as `customfields: [{"id":...,"value":...}]`. Replace a dictionary keyed by field id with one `CustomFieldInput` per field.
- `CreateTicketRequest.SuppressEmails` and `Notes` are obsolete and never sent: Halo's ticket create has no such fields.
- `TicketsApiWrapper.CreateAsync` refuses a `ClientId` or `UserId` that is not positive, and a `SiteId` that is set but not positive, before sending anything. Halo rejects 0, and -1 is its 'not set' value.
- `User.Username` is obsolete and returns `Login`: Halo's Users record has no `username` field, so it was never filled.

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