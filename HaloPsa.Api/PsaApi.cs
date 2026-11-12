using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using Refit;

namespace HaloPsa.Api;

/// <summary>
/// Implementation of PSA API module
/// </summary>
internal sealed class PsaApi(HttpClient _httpClient, bool? readOnly = null) : IPsaApi
{
	private static readonly RefitSettings _refitSettings = new()
	{
		ExceptionFactory = ConvertApiExceptionToHaloApiException
	};

	public TicketsApiWrapper Tickets { get; } = new Lazy<TicketsApiWrapper>(() => new TicketsApiWrapper(RestService.For<ITicketsApi>(_httpClient, _refitSettings), readOnly)).Value;
	public TicketTypesApiWrapper TicketTypes { get; } = new Lazy<TicketTypesApiWrapper>(() => new TicketTypesApiWrapper(RestService.For<ITicketTypesRefitApi>(_httpClient, _refitSettings))).Value;
	public UsersApiWrapper Users { get; } = new Lazy<UsersApiWrapper>(() => new UsersApiWrapper(RestService.For<IUsersRefitApi>(_httpClient, _refitSettings))).Value;
	public ClientsApiWrapper Clients { get; } = new Lazy<ClientsApiWrapper>(() => new ClientsApiWrapper(RestService.For<IClientsRefitApi>(_httpClient, _refitSettings))).Value;
	public AssetsApiWrapper Assets { get; } = new Lazy<AssetsApiWrapper>(() => new AssetsApiWrapper(RestService.For<IAssetsRefitApi>(_httpClient, _refitSettings))).Value;
	public ProjectsApiWrapper Projects { get; } = new Lazy<ProjectsApiWrapper>(() => new ProjectsApiWrapper(RestService.For<IProjectsRefitApi>(_httpClient, _refitSettings))).Value;
	public StatusesApiWrapper Statuses { get; } = new Lazy<StatusesApiWrapper>(() => new StatusesApiWrapper(RestService.For<IStatusesApi>(_httpClient, _refitSettings))).Value;
	public PrioritiesApiWrapper Priorities { get; } = new PrioritiesApiWrapper(RestService.For<IPrioritiesApi>(_httpClient, _refitSettings));
	public TeamsApiWrapper Teams { get; } = new Lazy<TeamsApiWrapper>(() => new TeamsApiWrapper(RestService.For<ITeamsApi>(_httpClient, _refitSettings))).Value;
	public CategoriesApiWrapper Categories { get; } = new Lazy<CategoriesApiWrapper>(() => new CategoriesApiWrapper(RestService.For<ICategoriesApi>(_httpClient, _refitSettings))).Value;
	public ActionsApiWrapper Actions { get; } = new ActionsApiWrapper(RestService.For<IActionsApi>(_httpClient, _refitSettings));
	public SitesApiWrapper Sites { get; } = new Lazy<SitesApiWrapper>(() => new SitesApiWrapper(RestService.For<ISitesApi>(_httpClient, _refitSettings))).Value;

	/// <summary>
	/// Converts Refit ApiExceptions to appropriate HaloApiExceptions
	/// </summary>
	/// <param name="httpResponseMessage">The HTTP response message</param>
	/// <returns>The appropriate HaloApiException or null if no exception should be thrown</returns>
	private static async ValueTask<Exception?> ConvertApiExceptionToHaloApiException(HttpResponseMessage httpResponseMessage)
	{
		if (httpResponseMessage.IsSuccessStatusCode)
		{
			return null;
		}

		var statusCode = (int)httpResponseMessage.StatusCode;
		var requestUrl = httpResponseMessage.RequestMessage?.RequestUri?.ToString();
		var requestMethod = httpResponseMessage.RequestMessage?.Method.Method;

		var content = await httpResponseMessage.Content.ReadAsStringAsync();
		var (details, validationErrors) = ApiErrorParser.ParseJsonContent(content);
		var message = ApiErrorParser.ReadStringBodyMessage(content)
			?? httpResponseMessage.ReasonPhrase
			?? $"API request failed with status {statusCode}";

		var errorContext = new HaloApiErrorContext
		{
			StatusCode = statusCode,
			Details = details,
			RequestUrl = requestUrl,
			RequestMethod = requestMethod
		};

		return ApiErrorParser.CreateExceptionForStatusCode(
			statusCode, message, validationErrors,
			requestUrl, httpResponseMessage.Headers, errorContext);
	}
}
