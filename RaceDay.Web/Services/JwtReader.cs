using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class JwtInfo
{
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>
/// Reads the claims of the JWT issued by the Part 2 API. The signature is not
/// validated here because the API validates the token on every request.
/// </summary>
public static class JwtReader
{
    private const string RoleUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    private const string IdUri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    private const string NameUri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
    private const string EmailUri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

    public static JwtInfo Read(string? token)
    {
        var info = new JwtInfo();
        if (string.IsNullOrWhiteSpace(token)) return info;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return info;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? Find(params string[] names)
            {
                foreach (var n in names)
                {
                    if (!root.TryGetProperty(n, out var v)) continue;
                    if (v.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in v.EnumerateArray()) return item.ToString();
                        continue;
                    }
                    return v.ToString();
                }
                return null;
            }

            info.UserId = Find("nameid", "sub", IdUri, "uid", "userId");
            info.Role = Find("role", RoleUri, "roles");
            info.Name = Find("unique_name", "name", NameUri, "given_name");
            info.Email = Find("email", EmailUri);

            if (root.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
            {
                info.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
        }
        catch (Exception)
        {
            return new JwtInfo();
        }

        return info;
    }
}
