using System.Net;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

public class RaceDayApiTests
{
    [Fact]
    public async Task Login_returns_the_token_on_success()
    {
        var (api, _) = TestFactory.CreateApi(HttpStatusCode.OK, """{"token":"abc.def.ghi"}""");

        var result = await api.LoginAsync("a@b.co", "Password1");

        Assert.True(result.Success);
        Assert.Equal("abc.def.ghi", result.Data);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task Login_failure_gives_a_friendly_message_and_does_not_throw(HttpStatusCode status)
    {
        var (api, _) = TestFactory.CreateApi(status, """{"message":"Invalid credentials"}""");

        var result = await api.LoginAsync("a@b.co", "wrong");

        Assert.False(result.Success);
        Assert.Contains("incorrect", result.Error);
    }

    [Fact]
    public async Task Authenticated_calls_send_the_bearer_token()
    {
        var session = new FakeSession { Token = "my-jwt" };
        var (api, handler) = TestFactory.CreateApi(HttpStatusCode.OK, "[]", session);

        await api.GetMyEnrolmentsAsync();

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("my-jwt", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Public_calls_do_not_send_a_token()
    {
        var session = new FakeSession { Token = "my-jwt" };
        var (api, handler) = TestFactory.CreateApi(HttpStatusCode.OK, "[]", session);

        await api.GetEventsAsync();

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task Rejected_token_raises_unauthorized_so_the_user_is_signed_out()
    {
        var session = new FakeSession { Token = "expired" };
        var (api, _) = TestFactory.CreateApi(HttpStatusCode.Unauthorized, "", session);

        await Assert.ThrowsAsync<ApiUnauthorizedException>(() => api.GetMyEnrolmentsAsync());
    }

    [Fact]
    public async Task Events_are_mapped_from_the_api_response()
    {
        var (api, _) = TestFactory.CreateApi(HttpStatusCode.OK,
            """[{"id":1,"name":"Dusi 10K","eventDate":"2030-01-01T05:00:00Z","distanceKm":10,"eventType":"Run"}]""");

        var result = await api.GetEventsAsync();

        Assert.True(result.Success);
        var ev = Assert.Single(result.Data!);
        Assert.Equal("Dusi 10K", ev.Name);
    }

    [Fact]
    public async Task Unreachable_api_returns_a_clear_error()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("down"));
        var api = new RaceDayApi(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") }, new FakeSession());

        var result = await api.GetEventsAsync();

        Assert.False(result.Success);
        Assert.Equal(RaceDayApi.ServiceUnavailable, result.Error);
    }

    [Fact]
    public void Validation_problem_details_are_flattened_into_one_message()
    {
        var body = """{"title":"One or more validation errors occurred.","errors":{"Password":["Too short."],"Email":["Already used."]}}""";

        var message = RaceDayApi.ExtractError(body, HttpStatusCode.BadRequest);

        Assert.Contains("Too short.", message);
        Assert.Contains("Already used.", message);
    }

    [Fact]
    public void Empty_error_body_falls_back_to_status_message() =>
        Assert.Contains("permission", RaceDayApi.ExtractError("", HttpStatusCode.Forbidden));
}
