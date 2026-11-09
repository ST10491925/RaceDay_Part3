using System.Net;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

public class FakeSession : IUserSession
{
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public string? DisplayName { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public bool IsOrganiser => IsAuthenticated && Role == "Organiser";
    public bool IsParticipant => IsAuthenticated && Role != "Organiser";
    public void SignIn(string token, string? fallbackName = null) { Token = token; }
    public void UpdateDisplayName(string name) { DisplayName = name; }
    public void SignOut() { Token = null; Role = null; }
}

public class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
    public HttpRequestMessage? LastRequest { get; private set; }

    public StubHandler(HttpStatusCode status, string body)
        : this(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") }) { }

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_respond(request));
    }
}

public static class TestFactory
{
    public static (RaceDayApi Api, StubHandler Handler) CreateApi(
        HttpStatusCode status, string body, IUserSession? session = null)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        return (new RaceDayApi(http, session ?? new FakeSession()), handler);
    }

    public static string MakeToken(Dictionary<string, object> claims)
    {
        var json = JsonSerializer.Serialize(claims);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"header.{payload}.signature";
    }

    public static JsonElement Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
