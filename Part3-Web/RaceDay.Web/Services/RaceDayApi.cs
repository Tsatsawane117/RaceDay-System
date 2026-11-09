using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;


public class RaceDayApi : IRaceDayApi
{
    private const string ApiCookieName = ".RaceDay.Session";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApi> _logger;

    public RaceDayApi(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApi> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    //  authentication

    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password) =>
        await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", Body(new { email, password }));

    public async Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest request) =>
        await SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", Body(request));

    public async Task<ApiResult<MessageResponse>> LogoutAsync() =>
        await SendAsync<MessageResponse>(HttpMethod.Post, "api/auth/logout");

    //  profile

    public async Task<ApiResult<UserDto>> GetProfileAsync() =>
        await SendAsync<UserDto>(HttpMethod.Get, "api/users/me");

    public async Task<ApiResult<UserDto>> UpdateProfileAsync(string fullName, string? contactNumber) =>
        await SendAsync<UserDto>(HttpMethod.Put, "api/users/me", Body(new { fullName, contactNumber }));

    public async Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file) =>
        await SendAsync<UserDto>(HttpMethod.Post, "api/users/me/picture", FileContent(file));

    //  events

    public async Task<ApiResult<List<EventDto>>> GetEventsAsync(string? eventType = null)
    {
        var url = string.IsNullOrWhiteSpace(eventType)
            ? "api/events"
            : $"api/events?eventType={Uri.EscapeDataString(eventType)}";
        return await SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public async Task<ApiResult<EventDto>> GetEventAsync(int id) =>
        await SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");

    public async Task<ApiResult<List<EventSummaryDto>>> GetMyEventsAsync() =>
        await SendAsync<List<EventSummaryDto>>(HttpMethod.Get, "api/events/mine");

    public async Task<ApiResult<EventDto>> CreateEventAsync(EventRequest request) =>
        await SendAsync<EventDto>(HttpMethod.Post, "api/events", Body(request));

    public async Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest request) =>
        await SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", Body(request));

    public async Task<ApiResult<MessageResponse>> DeleteEventAsync(int id) =>
        await SendAsync<MessageResponse>(HttpMethod.Delete, $"api/events/{id}");

    public async Task<ApiResult<EventDto>> UploadBannerAsync(int eventId, IFormFile file) =>
        await SendAsync<EventDto>(HttpMethod.Post, $"api/events/{eventId}/banner", FileContent(file));

    //  categories

    public async Task<ApiResult<CategoryDto>> AddCategoryAsync(int eventId, CategoryRequest request) =>
        await SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", Body(request));

    public async Task<ApiResult<MessageResponse>> DeleteCategoryAsync(int categoryId) =>
        await SendAsync<MessageResponse>(HttpMethod.Delete, $"api/categories/{categoryId}");

    //  enrolments and payments

    public async Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId) =>
        await SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/categories/{categoryId}/enrol");

    public async Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        await SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/users/me/enrolments");

    public async Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        await SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public async Task<ApiResult<MessageResponse>> CancelEnrolmentAsync(int enrolmentId) =>
        await SendAsync<MessageResponse>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    public async Task<ApiResult<PaymentDto>> PayAsync(int enrolmentId, decimal amount, string paymentMethod) =>
        await SendAsync<PaymentDto>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/payments",
            Body(new { amount, paymentMethod }));

    //  results

    public async Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) =>
        await SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results");

    public async Task<ApiResult<ResultDto>> CaptureResultAsync(ResultRequest request) =>
        await SendAsync<ResultDto>(HttpMethod.Post, "api/results", Body(request));

    public async Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync() =>
        await SendAsync<List<MyResultDto>>(HttpMethod.Get, "api/users/me/results");

    //  plumbing

    private static HttpContent Body(object value) => JsonContent.Create(value, value.GetType(), options: Json);

    private static MultipartFormDataContent FileContent(IFormFile file)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", file.FileName);
        return form;
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var cookie = Session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.Add("Cookie", cookie);
            }

            using var response = await _http.SendAsync(request);
            CaptureApiCookie(response);

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    data = JsonSerializer.Deserialize<T>(body, Json);
                }

                return new ApiResult<T> { Success = true, Data = data, StatusCode = (int)response.StatusCode };
            }

            return Failure<T>((int)response.StatusCode, ExtractError(body, response.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the RaceDay API at {Url}", url);
            return Failure<T>(503, "The RaceDay service is not available right now. Please try again in a moment.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "The RaceDay API timed out at {Url}", url);
            return Failure<T>(504, "The RaceDay service took too long to respond. Please try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unexpected response from the RaceDay API at {Url}", url);
            return Failure<T>(502, "We received an unexpected response from the RaceDay service.");
        }
    }

    private static ApiResult<T> Failure<T>(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Error = message };

    private void CaptureApiCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var value in values)
        {
            if (value.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
            {
                Session.SetString(SessionKeys.ApiCookie, value.Split(';')[0]);
            }
        }
    }


    private static string ExtractError(string body, HttpStatusCode status)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString()!;
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var field in errors.EnumerateObject())
                    {
                        foreach (var item in field.Value.EnumerateArray())
                        {
                            var text = item.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                messages.Add(text);
                            }
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join(" ", messages);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // The body was not JSON: use the generic message below.
        }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}
