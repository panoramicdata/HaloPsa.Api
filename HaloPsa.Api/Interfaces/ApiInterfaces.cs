namespace HaloPsa.Api.Interfaces;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HaloPsa.Api.Models.Assets;
using HaloPsa.Api.Models.Clients;
using HaloPsa.Api.Models.Projects;
using HaloPsa.Api.Models.TicketTypes;
using HaloPsa.Api.Models.Users;
using Refit;

/// <summary>Interface for user management operations</summary>
public interface IUsersApi
{
    /// <summary>
    /// Get all users - Convenience method that returns unwrapped array
    /// </summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Get all users - Returns wrapped response with metadata
    /// </summary>
    Task<UsersResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a user by ID
    /// </summary>
    Task<User> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Interface for client management operations</summary>
public interface IClientsApi
{
    /// <summary>
    /// Get all clients - Convenience method that returns unwrapped array
    /// </summary>
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a client by ID
    /// </summary>
    Task<Client> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get all clients - Returns wrapped response with metadata
    /// </summary>
    Task<ClientsResponse> GetResponseAsync(CancellationToken cancellationToken);
}

/// <summary>Interface for ticket type management operations</summary> 
public interface ITicketTypesApi
{
    /// <summary>
    /// Get all ticket types - Returns direct array (no wrapper)
    /// </summary>
    Task<IReadOnlyList<TicketType>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a ticket type by ID
    /// </summary>
    Task<TicketType> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Interface for asset management operations</summary>
public interface IAssetsApi
{
    /// <summary>
    /// Get all assets - Convenience method that returns unwrapped array
    /// </summary>
    Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Get all assets - Returns wrapped response with metadata
    /// </summary>
    Task<AssetsResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get an asset by ID
    /// </summary>
    Task<Asset> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Interface for project management operations</summary>
public interface IProjectsApi
{
    /// <summary>
    /// Get all projects - Convenience method that returns unwrapped array
    /// </summary>
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Get all projects - Returns wrapped response with metadata
    /// </summary>
    Task<ProjectsResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a project by ID
    /// </summary>
    Task<Project> GetByIdAsync(int id, CancellationToken cancellationToken);
}

// Internal Refit interfaces - these have the HTTP attributes
/// <summary>Internal Refit interface for Users API</summary>
public interface IUsersRefitApi
{
    /// <summary>Gets all users with response wrapper</summary>
    [Get("/api/Users")]
    Task<UsersResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>Gets a user by ID</summary>
    [Get("/api/Users/{id}")]
    Task<User> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Internal Refit interface for Clients API</summary>
public interface IClientsRefitApi
{
    /// <summary>Gets all clients with response wrapper</summary>
    [Get("/api/Client")]
    Task<ClientsResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>Gets a client by ID</summary>
    [Get("/api/Client/{id}")]
    Task<Client> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Internal Refit interface for TicketTypes API</summary>
public interface ITicketTypesRefitApi
{
    /// <summary>Gets all ticket types</summary>
    [Get("/api/TicketType")]
    Task<IReadOnlyList<TicketType>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Gets a ticket type by ID</summary>
    [Get("/api/TicketType/{id}")]
    Task<TicketType> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Internal Refit interface for Assets API</summary>
public interface IAssetsRefitApi
{
    /// <summary>Gets all assets with response wrapper</summary>
    [Get("/api/Asset")]
    Task<AssetsResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>Gets an asset by ID</summary>
    [Get("/api/Asset/{id}")]
    Task<Asset> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Internal Refit interface for Projects API</summary>
public interface IProjectsRefitApi
{
    /// <summary>Gets all projects with response wrapper</summary>
    [Get("/api/Projects")]
    Task<ProjectsResponse> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>Gets a project by ID</summary>
    [Get("/api/Projects/{id}")]
    Task<Project> GetByIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Interface for attachment operations</summary>
public interface IAttachmentsApi { }

/// <summary>Interface for reporting operations</summary>
public interface IReportsApi { }

/// <summary>Interface for knowledge base operations</summary>
public interface IKnowledgeBaseApi { }

/// <summary>Interface for service catalog operations</summary>
public interface IServiceCatalogApi { }

/// <summary>Interface for workflow operations</summary>
public interface IWorkflowsApi { }

/// <summary>Interface for approval operations</summary>
public interface IApprovalsApi { }

/// <summary>Interface for configuration operations</summary>
public interface IConfigurationApi { }

/// <summary>Interface for integration operations</summary>
public interface IIntegrationApi { }

/// <summary>Interface for audit operations</summary>
public interface IAuditApi { }

/// <summary>Interface for custom field operations</summary>
public interface ICustomFieldsApi { }