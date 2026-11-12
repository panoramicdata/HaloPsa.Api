# Halo PSA API .NET Library

[![NuGet Version](https://img.shields.io/nuget/v/HaloPsa.Api)](https://www.nuget.org/packages/HaloPsa.Api)
[![NuGet Downloads](https://img.shields.io/nuget/dt/HaloPsa.Api)](https://www.nuget.org/packages/HaloPsa.Api)
[![Build Status](https://img.shields.io/github/actions/workflow/status/panoramicdata/HaloPsa.Api/publish-nuget.yml)](https://github.com/panoramicdata/HaloPsa.Api/actions)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/a6c135d1c93d4d818e770f149385a149)](https://app.codacy.com/gh/panoramicdata/HaloPsa.Api/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive, modern .NET library for interacting with the [Halo PSA](https://halopsa.com/) API. This library provides full coverage of the Halo PSA API with a clean, intuitive interface using modern C# patterns and best practices.

## 📚 Official Documentation

- **API Documentation**: [https://halo.haloservicedesk.com/apidoc/info](https://halo.haloservicedesk.com/apidoc/info)
- **Authentication Guide**: [https://halo.haloservicedesk.com/apidoc/authentication/clientcredentials](https://halo.haloservicedesk.com/apidoc/authentication/clientcredentials)
- **Halo PSA Official Site**: [https://haloservicedesk.com/halopsa](https://haloservicedesk.com/halopsa)

## Features

- 🎯 **Complete API Coverage** - Full support for all Halo PSA endpoints
- 🚀 **Modern .NET** - Built for .NET 9 with modern C# features
- 🔒 **Type Safety** - Strongly typed models and responses
- 📝 **Comprehensive Logging** - Built-in logging and request/response interception
- 🔄 **Retry Logic** - Automatic retry with exponential backoff; a POST is retried only on 429 Too Many Requests
- 📖 **Rich Documentation** - IntelliSense-friendly XML documentation
- ✅ **Thoroughly Tested** - Comprehensive unit and integration tests
- ⚡ **High Performance** - Optimized for efficiency and low memory usage

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package HaloPsa.Api
```

Or via Package Manager Console:

```powershell
Install-Package HaloPsa.Api
```

## Quick Start

### 1. Authentication Setup

Halo API uses **OAuth2 Client Credentials flow** for authentication. You'll need:

1. **Halo Account Name** - Your instance identifier (e.g., "yourcompany" for "yourcompany.halopsa.com")
2. **Client ID** - Your application's registered client ID (GUID format)
3. **Client Secret** - Your application's client secret (two concatenated GUIDs)

Refer to the [official authentication documentation](https://halo.haloservicedesk.com/apidoc/authentication/clientcredentials) for detailed guidance on obtaining these credentials.

#### Creating API Credentials in Halo
1. Log into your Halo instance as an administrator
2. Navigate to **Configuration** → **Integrations** → **Halo API**
3. Click **New** to create a new API application
4. Configure the application settings:
   - **Application Name**: Your application name
   - **Authentication Method**: Client Credentials
   - **Permissions**: Select appropriate scopes for your use case
5. Save the application to generate your **Client ID** and **Client Secret**

```csharp
using HaloPsa.Api;

var options = new HaloClientOptions
{
    Account = "your-account-name",        // e.g., "yourcompany" 
    ClientId = "your-client-id-guid",     // Generated Client ID (GUID)
    ClientSecret = "your-client-secret"   // Generated Client Secret (two concatenated GUIDs)
};

var client = new HaloClient(options);
```

### 2. Basic Usage Examples

#### Working with Tickets

```csharp
// Use a CancellationToken for all async operations
using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

// Get all open tickets
var filter = new TicketFilter
{
    Status = TicketStatus.Open,
    Count = 50
};

var tickets = await client.Psa.Tickets.GetAllAsync(filter, cancellationToken);

foreach (var ticket in tickets.Tickets)
{
    Console.WriteLine($"Ticket #{ticket.Id}: {ticket.Summary}");
}

// Get a specific ticket with details
var ticket = await client.Psa.Tickets.GetByIdAsync(12345, includeDetails: true, cancellationToken);
Console.WriteLine($"Ticket: {ticket.Summary}");
Console.WriteLine($"Status: {ticket.Status}");
Console.WriteLine($"Assigned to: {ticket.AssignedAgent?.Name}");

// Create a new ticket
var newTicket = new CreateTicketRequest
{
    Summary = "New ticket from API",
    Details = "This ticket was created using the HaloPsa.Api library",
    ClientId = 123,
    UserId = 456,
    TicketTypeId = 1
};

var createdTicket = await client.Psa.Tickets.CreateAsync(newTicket, cancellationToken);
Console.WriteLine($"Created ticket #{createdTicket.TicketId}");
```

#### Working with Users

```csharp
// Search for users (one page; set Paginate and PageNo to read further pages)
var userFilter = new UserFilter
{
    Search = "john.doe",
    IncludeInactive = false
};

var users = await client.Psa.Users.GetAllAsync(userFilter, cancellationToken);

// Get user details
var user = await client.Psa.Users.GetByIdAsync(123, cancellationToken);
Console.WriteLine($"User: {user.Name} ({user.EmailAddress})");
```

#### Working with Clients

```csharp
// Get every active client, paging to the end
var clientFilter = new ClientFilter
{
    IncludeInactive = false,
    PageSize = 100
};

var clients = await client.Psa.Clients.GetAllPagedAsync(clientFilter, cancellationToken);

// Get client with additional details
var clientDetails = await client.Psa.Clients.GetByIdAsync(123, includeDetails: true, cancellationToken);
Console.WriteLine($"Client: {clientDetails.Name}");
```

### 3. Advanced Configuration

#### Custom HTTP Configuration

```csharp
var options = new HaloClientOptions
{
    Account = "your-account",
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret",
    
    // Custom timeout
    RequestTimeout = TimeSpan.FromSeconds(30),
    
    // Custom retry policy
    MaxRetryAttempts = 3,
    RetryDelay = TimeSpan.FromSeconds(1)
};

var client = new HaloClient(options);
```

#### Logging Configuration

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Create a service collection with logging
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<HaloClient>>();

var options = new HaloClientOptions
{
    // ... authentication details
    Logger = logger,
    EnableRequestLogging = true,
    EnableResponseLogging = true
};

var client = new HaloClient(options);
```

### 4. Authentication Troubleshooting

If you're experiencing authentication issues:

1. **Verify Client Credentials**: Ensure your Client ID and Client Secret are correct and haven't been regenerated
2. **Check API Application Status**: Verify the API application is enabled in Halo Configuration
3. **Validate Client ID Format**: Ensure the Client ID is a valid GUID format
4. **Validate Client Secret Format**: Ensure the Client Secret is in the correct format (two concatenated GUIDs)
5. **Check Permissions**: Verify your API application has the necessary permissions/scopes
6. **Network Connectivity**: Ensure your application can reach the Halo API endpoints

### 5. Pagination and Large Result Sets

```csharp
// Handle pagination automatically
var allTickets = new List<Ticket>();
var pageSize = 100;
var pageNumber = 1;

do
{
    var filter = new TicketFilter
    {
        PageSize = pageSize,
        PageNumber = pageNumber,
        IncludeActive = true
    };
    
    var response = await client.Psa.Tickets.GetAllAsync(filter, cancellationToken);
    allTickets.AddRange(response.Tickets);
    
    pageNumber++;
    
    // Continue while we got a full page
} while (response.Tickets.Count == pageSize);

Console.WriteLine($"Retrieved {allTickets.Count} total tickets");
```

### 6. Error Handling

```csharp
try
{
    var ticket = await client.Psa.Tickets.GetByIdAsync(99999, cancellationToken);
}
catch (HaloNotFoundException ex)
{
    Console.WriteLine($"Ticket not found: {ex.Message}");
}
catch (HaloAuthenticationException ex)
{
    Console.WriteLine($"Authentication failed: {ex.Message}");
    Console.WriteLine("Check your username, password, and API permissions");
}
catch (HaloApiException ex)
{
    Console.WriteLine($"API error: {ex.Message}");
    Console.WriteLine($"Status code: {ex.StatusCode}");
    Console.WriteLine($"Error details: {ex.ErrorDetails}");
}
```

## API Coverage

This library provides comprehensive coverage of the Halo PSA API, organized into logical groups. For complete API endpoint documentation, refer to the [official API documentation](https://halo.haloservicedesk.com/apidoc/info).

### PSA Module (`client.Psa`)
- **Tickets** - Full CRUD operations, filtering, actions, and workflow
- **Users** - User management, authentication, and permissions
- **Clients** - Client and site management
- **Assets** - Asset tracking and configuration management
- **Projects** - Project management and time tracking
- **Reports** - Reporting and analytics

### ServiceDesk Module (`client.ServiceDesk`)
- **Knowledge Base** - Article management and search
- **Service Catalog** - Service requests and approvals
- **Assets** - IT asset management
- **Workflows** - Custom workflows and automation

### System Module (`client.System`)
- **Configuration** - System settings and customization
- **Integration** - Third-party system integrations
- **Audit** - Audit logs and activity tracking

## Configuration Options

The `HaloClientOptions` class provides extensive configuration:

```csharp
public class HaloClientOptions
{
    // Required authentication
    public required string Account { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    
    // Optional configuration
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxRetryAttempts { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public ILogger? Logger { get; init; } = null;
    
    // Advanced options
    public bool EnableRequestLogging { get; init; } = false;
    public bool EnableResponseLogging { get; init; } = false;
    public IReadOnlyDictionary<string, string> DefaultHeaders { get; init; } = new Dictionary<string, string>();
    public bool UseExponentialBackoff { get; init; } = true;
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
}
```

## API Reference

For detailed API endpoint documentation, parameters, and response formats, please refer to the official resources:

- 📖 **[Halo API Documentation](https://halo.haloservicedesk.com/apidoc/info)** - Complete API reference
- 🔐 **[Authentication Guide](https://halo.haloservicedesk.com/apidoc/authentication/clientcredentials)** - How to obtain and use API credentials
- 🌐 **[Halo Service Desk](https://haloservicedesk.com/)** - Official product documentation

## Contributing

We welcome contributions from the community! Here's how you can help:

### Development Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/panoramicdata/HaloPsa.Api.git
   cd HaloPsa.Api
   ```

2. **Install .NET 9 SDK**:
   Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

3. **Set up User Secrets for testing**:
   ```bash
   cd HaloPsa.Api.Test
   dotnet user-secrets init
   dotnet user-secrets set "HaloApi:Account" "your-test-account"
   dotnet user-secrets set "HaloApi:ClientId" "your-test-client-id"
   dotnet user-secrets set "HaloApi:ClientSecret" "your-test-client-secret"
   ```

4. **Build and test**:
   ```bash
   dotnet build
   dotnet test
   ```

### Coding Standards

- **Follow the project's coding standards** as defined in `copilot-instructions.md`
- **Use modern C# patterns** (primary constructors, collection expressions, etc.)
- **Maintain zero warnings policy** - all code must compile without warnings
- **Write comprehensive tests** - both unit and integration tests
- **Document public APIs** - use XML documentation comments

### Pull Request Process

1. **Fork the repository** and create a feature branch
2. **Follow the implementation plan** in `Specification/ImplementationPlan.md`
3. **Write tests** for all new functionality
4. **Ensure all tests pass** including integration tests
5. **Update documentation** as needed
6. **Submit a pull request** with a clear description of changes

### Issue Reporting

When reporting issues:

- **Use the issue templates** provided in the repository
- **Include minimal reproduction code** when possible
- **Specify the library version** and .NET version
- **Include relevant error messages** and stack traces

### Development Guidelines

- **API-First Approach**: All new endpoints should be defined in interfaces first
- **Test-Driven Development**: Write tests before implementing functionality
- **Documentation**: Update both XML docs and README examples
- **Performance**: Consider performance implications of new features
- **Backward Compatibility**: Maintain compatibility when possible

## Support

- **Official Documentation**: [Halo API Docs](https://halo.haloservicedesk.com/apidoc/info)
- **GitHub Issues**: [Report Issues](https://github.com/panoramicdata/HaloPsa.Api/issues)
- **GitHub Discussions**: [Community Support](https://github.com/panoramicdata/HaloPsa.Api/discussions)
- **Halo Support**: Contact Halo Service Desk for API access and account issues

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Copyright

Copyright © 2025 Panoramic Data Limited. All rights reserved.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes and releases
