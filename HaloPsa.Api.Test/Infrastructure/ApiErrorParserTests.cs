using AwesomeAssertions;
using AwesomeAssertions.Specialized;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Models.Tickets;
using Refit;
using System.Net;
using System.Text;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Reads Halo's error bodies. Halo answers some errors with a JSON string or array rather than an object,
/// and the error parser must still raise the matching <see cref="HaloApiException"/> with the status.
/// </summary>
public class ApiErrorParserTests
{
	private const string _stringBody = "\"Category is mandatory\\n\"";

	private static readonly CreateTicketRequest _validRequest = new() { Summary = "S", ClientId = 21, UserId = 9 };

	[Fact]
	public async Task CreateAsync_WhenHaloAnswers400WithAStringBody_RaisesTheBadRequestErrorWithHalosMessage()
	{
		var thrown = (await CreateExpectingAsync<HaloBadRequestException>(_stringBody, HttpStatusCode.BadRequest)).WithMessage("*Category is mandatory*").Which;
		_ = thrown.StatusCode.Should().Be(400);
		_ = thrown.Message.Should().NotContain("\n");
		_ = thrown.Details.Should().ContainKey("message").WhoseValue.Should().Be("Category is mandatory");
	}

	[Fact]
	public async Task CreateAsync_WhenHaloAnswers400WithABlankStringBody_KeepsTheReasonPhrase()
		=> _ = (await CreateExpectingAsync<HaloBadRequestException>("\"  \\n\"", HttpStatusCode.BadRequest)).WithMessage("Bad request: Bad Request");

	[Fact]
	public async Task CreateAsync_WhenHaloAnswers400WithAStringBodyThatCannotBeDecoded_RaisesTheBadRequestError()
	{
		var thrown = (await CreateExpectingAsync<HaloBadRequestException>("\"\\ud800\"", HttpStatusCode.BadRequest)).WithMessage("Bad request: Bad Request").Which;
		_ = thrown.StatusCode.Should().Be(400);
		_ = thrown.Details.Should().ContainKey("rawContent");
	}

	[Fact]
	public async Task CreateAsync_WhenHaloAnswers500WithAnArrayBody_RaisesTheServerError()
	{
		var thrown = (await CreateExpectingAsync<HaloServerException>("[]", HttpStatusCode.InternalServerError)).Which;
		_ = thrown.StatusCode.Should().Be(500);
	}

	[Fact]
	public async Task CreateAsync_WhenHaloAnswers400WithAnObjectBody_KeepsTheReasonPhraseAndTheDetails()
	{
		var thrown = (await CreateExpectingAsync<HaloBadRequestException>("""{"message":"Category is mandatory","errors":["a","b"]}""", HttpStatusCode.BadRequest)).WithMessage("Bad request: Bad Request").Which;
		_ = thrown.ValidationErrors.Should().Equal("a", "b");
		_ = thrown.Details.Should().ContainKey("message").WhoseValue.Should().Be("Category is mandatory");
	}

	[Fact]
	public async Task ErrorHandler_WhenTheApiExceptionBodyIsAString_RaisesTheBadRequestErrorWithHalosMessage()
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/api/Tickets");
		using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		{
			Content = new StringContent(_stringBody, Encoding.UTF8, "application/json")
		};
		var apiException = await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
		using var invoker = new HttpMessageInvoker(new ErrorHandler(null) { InnerHandler = new ThrowingHandler(apiException) });

		var act = async () => await invoker.SendAsync(request, CancellationToken.None);

		var thrown = (await act.Should().ThrowExactlyAsync<HaloBadRequestException>()).WithMessage("*Category is mandatory*").Which;
		_ = thrown.StatusCode.Should().Be(400);
		_ = thrown.InnerException.Should().BeSameAs(apiException);
	}

	[Theory]
	[InlineData("\"text\"")]
	[InlineData("[]")]
	[InlineData("[1,2]")]
	[InlineData("42")]
	[InlineData("true")]
	[InlineData("null")]
	[InlineData("\"\\ud800\"")]
	public void ParseJsonContent_WhenTheRootIsNotAnObject_DoesNotThrow(string body)
	{
		var act = () => ApiErrorParser.ParseJsonContent(body);

		_ = act.Should().NotThrow();
	}

	[Fact]
	public void ParseJsonContent_WhenTheRootIsAString_ReadsItTrimmedAsTheMessage()
	{
		var (details, validationErrors) = ApiErrorParser.ParseJsonContent("\"  Category is mandatory\\n\"");

		_ = details.Should().Equal(new Dictionary<string, object?> { ["message"] = "Category is mandatory" });
		_ = validationErrors.Should().BeNull();
	}

	[Theory]
	[InlineData("[]")]
	[InlineData("[1,2]")]
	[InlineData("42")]
	[InlineData("true")]
	[InlineData("null")]
	public void ParseJsonContent_WhenTheRootIsNeitherAnObjectNorAString_KeepsTheRawContent(string body)
		=> AssertRawContent(body);

	[Fact]
	public void ParseJsonContent_WhenTheRootIsAnObject_ReadsItsPropertiesAndErrors()
	{
		var (details, validationErrors) = ApiErrorParser.ParseJsonContent("""{"message":"m","code":7,"ok":false,"errors":["a","b"]}""");

		_ = details.Should().ContainKey("message").WhoseValue.Should().Be("m");
		_ = details.Should().ContainKey("code").WhoseValue.Should().Be(7);
		_ = details.Should().ContainKey("ok").WhoseValue.Should().Be(false);
		_ = validationErrors.Should().Equal("a", "b");
	}

	[Fact]
	public void ParseJsonContent_WhenTheBodyIsNotJson_KeepsTheRawContent()
		=> AssertRawContent("<html>Gateway page</html>");

	private static async Task<ExceptionAssertions<TException>> CreateExpectingAsync<TException>(string body, HttpStatusCode status)
		where TException : Exception
	{
		var handler = new CapturingHandler(body) { StatusCode = status };
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);

		var act = async () => await psa.Tickets.CreateAsync(_validRequest, CancellationToken.None);

		return await act.Should().ThrowExactlyAsync<TException>();
	}

	private static void AssertRawContent(string body)
	{
		var (details, validationErrors) = ApiErrorParser.ParseJsonContent(body);

		_ = details.Should().Equal(new Dictionary<string, object?> { ["rawContent"] = body });
		_ = validationErrors.Should().BeNull();
	}

	private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();

			throw exception;
		}
	}
}
