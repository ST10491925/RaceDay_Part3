using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public interface IRaceDayApi
{
    Task<ApiResult<string>> LoginAsync(string email, string password);
    Task<ApiResult<bool>> RegisterAsync(RegisterForm form);

    Task<ApiResult<List<EventItem>>> GetEventsAsync();
    Task<ApiResult<EventItem>> GetEventAsync(int id);
    Task<ApiResult<int>> CreateEventAsync(EventForm form);
    Task<ApiResult<bool>> UpdateEventAsync(int id, EventForm form);
    Task<ApiResult<bool>> DeleteEventAsync(int id);
    Task<ApiResult<bool>> UploadEventBannerAsync(int id, IFormFile file);

    Task<ApiResult<List<CategoryItem>>> GetCategoriesAsync();
    Task<ApiResult<List<CategoryItem>>> GetEventCategoriesAsync(int eventId);
    Task<ApiResult<int>> CreateCategoryAsync(CategoryForm form);
    Task<ApiResult<bool>> AddCategoryToEventAsync(int eventId, int categoryId);

    Task<ApiResult<bool>> EnrolAsync(int eventId, int eventCategoryId);
    Task<ApiResult<List<EnrolmentItem>>> GetMyEnrolmentsAsync();
    Task<ApiResult<bool>> CancelEnrolmentAsync(int enrolmentId);
    Task<ApiResult<List<EnrolmentItem>>> GetEventEnrolmentsAsync(int eventId);

    Task<ApiResult<List<ResultItem>>> GetMyResultsAsync();
    Task<ApiResult<List<ResultItem>>> GetEventResultsAsync(int eventId);
    Task<ApiResult<bool>> SaveResultAsync(ResultForm form);

    Task<ApiResult<UserProfile>> GetProfileAsync();
    Task<ApiResult<bool>> UpdateProfileAsync(ProfileForm form);
    Task<ApiResult<bool>> UploadProfilePictureAsync(IFormFile file);
}

/// <summary>
/// Typed HTTP client for the RaceDay Part 2 API. The MVC project never touches
/// the database or Azure Blob Storage; every operation goes through this class.
/// </summary>
public class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IUserSession _session;

    public RaceDayApi(HttpClient http, IUserSession session)
    {
        _http = http;
        _session = session;
    }

    // ---------- Authentication ----------

    public async Task<ApiResult<string>> LoginAsync(string email, string password)
    {
        var res = await SendAsync(HttpMethod.Post, ApiRoutes.Login,
            Json(new { email, password }), auth: false);

        if (!res.Success)
        {
            var message = res.Status is 400 or 401
                ? "The email address or password is incorrect."
                : res.Error ?? "Sign in failed.";
            return ApiResult<string>.Fail(message, res.Status);
        }

        var token = Js.Str(res.Data, "token", "accessToken", "jwt", "jwtToken");
        return string.IsNullOrWhiteSpace(token)
            ? ApiResult<string>.Fail("The service did not return a sign-in token.", 502)
            : ApiResult<string>.Ok(token);
    }

    public async Task<ApiResult<bool>> RegisterAsync(RegisterForm form)
    {
        var res = await SendAsync(HttpMethod.Post, ApiRoutes.Register, Json(new
        {
            firstName = form.FirstName.Trim(),
            lastName = form.LastName.Trim(),
            email = form.Email.Trim(),
            phoneNumber = form.Phone,
            password = form.Password
        }), auth: false);
        return ToBool(res);
    }

    // ---------- Events ----------

    public async Task<ApiResult<List<EventItem>>> GetEventsAsync()
    {
        var res = await SendAsync(HttpMethod.Get, ApiRoutes.Events, auth: false);
        return ToList(res, ApiMapper.ToEvent);
    }

    public async Task<ApiResult<EventItem>> GetEventAsync(int id)
    {
        var res = await SendAsync(HttpMethod.Get, ApiRoutes.Event(id), auth: false);
        if (!res.Success) return ApiResult<EventItem>.Fail(res.Error ?? "Event not found.", res.Status);
        return ApiResult<EventItem>.Ok(ApiMapper.ToEvent(res.Data));
    }

    public async Task<ApiResult<int>> CreateEventAsync(EventForm form)
    {
        var res = await SendAsync(HttpMethod.Post, ApiRoutes.Events, Json(EventBody(form)));
        if (!res.Success) return ApiResult<int>.Fail(res.Error ?? "The event could not be created.", res.Status);
        return ApiResult<int>.Ok(Js.Int(res.Data, "id", "eventId") ?? 0);
    }

    public async Task<ApiResult<bool>> UpdateEventAsync(int id, EventForm form) =>
        ToBool(await SendAsync(HttpMethod.Put, ApiRoutes.Event(id), Json(EventBody(form))));

    public async Task<ApiResult<bool>> DeleteEventAsync(int id) =>
        ToBool(await SendAsync(HttpMethod.Delete, ApiRoutes.Event(id)));

    public async Task<ApiResult<bool>> UploadEventBannerAsync(int id, IFormFile file) =>
        ToBool(await SendAsync(HttpMethod.Post, ApiRoutes.EventBanner(id), FileContent(file)));

    private static object EventBody(EventForm f) => new
    {
        name = f.Name.Trim(),
        description = f.Description.Trim(),
        location = f.Location.Trim(),
        eventDate = f.Date,
        distanceKm = f.DistanceKm,
        eventType = f.EventType
    };

    // ---------- Categories ----------

    public async Task<ApiResult<List<CategoryItem>>> GetCategoriesAsync() =>
        ToList(await SendAsync(HttpMethod.Get, ApiRoutes.Categories, auth: false), ApiMapper.ToCategory);

    public async Task<ApiResult<List<CategoryItem>>> GetEventCategoriesAsync(int eventId) =>
        ToList(await SendAsync(HttpMethod.Get, ApiRoutes.EventCategories(eventId), auth: false), ApiMapper.ToCategory);

    public async Task<ApiResult<int>> CreateCategoryAsync(CategoryForm form)
    {
        var res = await SendAsync(HttpMethod.Post, ApiRoutes.Categories, Json(new
        {
            name = form.Name.Trim(),
            description = form.Description,
            categoryType = form.CategoryType,
            minAge = form.MinAge,
            maxAge = form.MaxAge,
            distanceKm = form.DistanceKm
        }));
        if (!res.Success) return ApiResult<int>.Fail(res.Error ?? "The category could not be created.", res.Status);
        return ApiResult<int>.Ok(Js.Int(res.Data, "id", "categoryId") ?? 0);
    }

    public async Task<ApiResult<bool>> AddCategoryToEventAsync(int eventId, int categoryId) =>
        ToBool(await SendAsync(HttpMethod.Post, ApiRoutes.EventCategories(eventId), Json(new { categoryId })));

    // ---------- Enrolments ----------

    public async Task<ApiResult<bool>> EnrolAsync(int eventId, int eventCategoryId) =>
        ToBool(await SendAsync(HttpMethod.Post, ApiRoutes.Enrolments,
            Json(new { eventId, eventCategoryId, categoryId = eventCategoryId })));

    public async Task<ApiResult<List<EnrolmentItem>>> GetMyEnrolmentsAsync() =>
        ToList(await SendAsync(HttpMethod.Get, ApiRoutes.MyEnrolments), ApiMapper.ToEnrolment);

    public async Task<ApiResult<bool>> CancelEnrolmentAsync(int enrolmentId) =>
        ToBool(await SendAsync(HttpMethod.Delete, ApiRoutes.Enrolment(enrolmentId)));

    public async Task<ApiResult<List<EnrolmentItem>>> GetEventEnrolmentsAsync(int eventId) =>
        ToList(await SendAsync(HttpMethod.Get, ApiRoutes.EventEnrolments(eventId)), ApiMapper.ToEnrolment);

    // ---------- Results ----------

    public async Task<ApiResult<List<ResultItem>>> GetMyResultsAsync() =>
        ToList(await SendAsync(HttpMethod.Get, ApiRoutes.MyResults), ApiMapper.ToResult);

    public async Task<ApiResult<List<ResultItem>>> GetEventResultsAsync(int eventId) =>
        ToList(await SendAsync(HttpMethod.Get, ApiRoutes.EventResults(eventId)), ApiMapper.ToResult);

    public async Task<ApiResult<bool>> SaveResultAsync(ResultForm form)
    {
        var body = Json(new
        {
            enrolmentId = form.EnrolmentId,
            finishTime = form.FinishTime.Trim(),
            position = form.Position
        });

        var res = form.ResultId is > 0
            ? await SendAsync(HttpMethod.Put, ApiRoutes.Result(form.ResultId.Value), body)
            : await SendAsync(HttpMethod.Post, ApiRoutes.Results, body);

        return ToBool(res);
    }

    // ---------- Profile ----------

    public async Task<ApiResult<UserProfile>> GetProfileAsync()
    {
        var res = await SendAsync(HttpMethod.Get, ApiRoutes.Me);
        if (!res.Success) return ApiResult<UserProfile>.Fail(res.Error ?? "Profile not available.", res.Status);
        return ApiResult<UserProfile>.Ok(ApiMapper.ToProfile(res.Data));
    }

    public async Task<ApiResult<bool>> UpdateProfileAsync(ProfileForm form) =>
        ToBool(await SendAsync(HttpMethod.Put, ApiRoutes.Me, Json(new
        {
            firstName = form.FirstName.Trim(),
            lastName = form.LastName.Trim(),
            phoneNumber = form.Phone
        })));

    public async Task<ApiResult<bool>> UploadProfilePictureAsync(IFormFile file) =>
        ToBool(await SendAsync(HttpMethod.Post, ApiRoutes.MyPicture, FileContent(file)));

    // ---------- Plumbing ----------

    private static HttpContent Json(object body) => JsonContent.Create(body, options: JsonOptions);

    private static MultipartFormDataContent FileContent(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(stream, "file", file.FileName);
        return content;
    }

    private static ApiResult<bool> ToBool(ApiResult<JsonElement> res) =>
        res.Success ? ApiResult<bool>.Ok(true, res.Status)
                    : ApiResult<bool>.Fail(res.Error ?? "The request failed.", res.Status);

    private static ApiResult<List<T>> ToList<T>(ApiResult<JsonElement> res, Func<JsonElement, T> map)
    {
        if (!res.Success) return ApiResult<List<T>>.Fail(res.Error ?? "The request failed.", res.Status);
        return ApiResult<List<T>>.Ok(Js.Items(res.Data).Select(map).ToList());
    }

    private async Task<ApiResult<JsonElement>> SendAsync(
        HttpMethod method, string path, HttpContent? content = null, bool auth = true)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };

        var token = _session.Token;
        if (auth && !string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Unauthorized && auth)
            {
                throw new ApiUnauthorizedException();
            }

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<JsonElement>.Fail(ExtractError(body, response.StatusCode), (int)response.StatusCode);
            }

            JsonElement data = default;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    data = doc.RootElement.Clone();
                }
                catch (JsonException)
                {
                    // A non-JSON success body (for example plain text) is treated as an empty payload.
                }
            }
            return ApiResult<JsonElement>.Ok(data, (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<JsonElement>.Fail(ServiceUnavailable, 503);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<JsonElement>.Fail(ServiceUnavailable, 504);
        }
    }

    public const string ServiceUnavailable =
        "The RaceDay service could not be reached. Check that the API is running and try again.";

    public static string ExtractError(string body, HttpStatusCode status)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    var errors = Js.Prop(root, "errors");
                    if (errors is { ValueKind: JsonValueKind.Object })
                    {
                        var messages = new List<string>();
                        foreach (var p in errors.Value.EnumerateObject())
                        {
                            if (p.Value.ValueKind == JsonValueKind.Array)
                                messages.AddRange(p.Value.EnumerateArray().Select(x => x.ToString()));
                            else
                                messages.Add(p.Value.ToString());
                        }
                        if (messages.Count > 0) return string.Join(" ", messages.Distinct());
                    }
                    if (errors is { ValueKind: JsonValueKind.Array })
                    {
                        var messages = errors.Value.EnumerateArray().Select(x => x.ToString()).ToList();
                        if (messages.Count > 0) return string.Join(" ", messages);
                    }

                    var text = Js.Str(root, "message", "error", "detail", "title");
                    if (!string.IsNullOrWhiteSpace(text)) return text!;
                }
                else if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString() ?? Fallback(status);
                }
            }
            catch (JsonException)
            {
                if (body.Length < 200 && !body.TrimStart().StartsWith('<')) return body.Trim();
            }
        }
        return Fallback(status);
    }

    private static string Fallback(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "The details provided were not accepted. Check them and try again.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.NotFound => "That item could not be found.",
        HttpStatusCode.Conflict => "That already exists or conflicts with existing data.",
        HttpStatusCode.UnprocessableEntity => "The details provided were not accepted. Check them and try again.",
        _ => "The RaceDay service reported a problem. Please try again."
    };
}
