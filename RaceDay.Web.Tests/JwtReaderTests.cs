using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

public class JwtReaderTests
{
    [Fact]
    public void Reads_role_id_and_name_from_short_claims()
    {
        var token = TestFactory.MakeToken(new()
        {
            ["nameid"] = "12", ["role"] = "Organiser", ["unique_name"] = "Sizwe Sigubudu",
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        });

        var info = JwtReader.Read(token);

        Assert.Equal("12", info.UserId);
        Assert.Equal("Organiser", info.Role);
        Assert.Equal("Sizwe Sigubudu", info.Name);
        Assert.NotNull(info.ExpiresAt);
    }

    [Fact]
    public void Reads_long_form_microsoft_claim_uris()
    {
        var token = TestFactory.MakeToken(new()
        {
            ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] = "5",
            ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] = "Participant"
        });

        var info = JwtReader.Read(token);

        Assert.Equal("5", info.UserId);
        Assert.Equal("Participant", info.Role);
    }

    [Fact]
    public void Reads_first_role_when_role_is_an_array()
    {
        var token = TestFactory.MakeToken(new() { ["role"] = new[] { "Organiser", "Participant" } });
        Assert.Equal("Organiser", JwtReader.Read(token).Role);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("a.@@@.c")]
    public void Malformed_tokens_return_empty_info(string? token)
    {
        var info = JwtReader.Read(token);
        Assert.Null(info.UserId);
        Assert.Null(info.Role);
    }
}
