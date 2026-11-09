namespace RaceDay.Web.Services;

public interface IUserSession
{
    string? Token { get; }
    string? UserId { get; }
    string? Role { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    bool IsOrganiser { get; }
    bool IsParticipant { get; }
    void SignIn(string token, string? fallbackName = null);
    void UpdateDisplayName(string name);
    void SignOut();
}

/// <summary>
/// Session-based authentication state. The JWT, user id, role and display name
/// are stored in server-side session and are cleared on logout.
/// </summary>
public class UserSession : IUserSession
{
    private const string TokenKey = "rd.token";
    private const string UserIdKey = "rd.uid";
    private const string RoleKey = "rd.role";
    private const string NameKey = "rd.name";
    private const string ExpiryKey = "rd.exp";

    private readonly IHttpContextAccessor _accessor;

    public UserSession(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession? Session => _accessor.HttpContext?.Session;

    public string? Token => IsExpired() ? null : Session?.GetString(TokenKey);
    public string? UserId => Session?.GetString(UserIdKey);
    public string? Role => Session?.GetString(RoleKey);
    public string? DisplayName => Session?.GetString(NameKey);

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public bool IsOrganiser =>
        IsAuthenticated && string.Equals(Role, "Organiser", StringComparison.OrdinalIgnoreCase);

    public bool IsParticipant => IsAuthenticated && !IsOrganiser;

    public void SignIn(string token, string? fallbackName = null)
    {
        var info = JwtReader.Read(token);
        var session = Session ?? throw new InvalidOperationException("Session is not available.");

        session.Clear();
        session.SetString(TokenKey, token);
        session.SetString(UserIdKey, info.UserId ?? "");
        session.SetString(RoleKey, string.IsNullOrWhiteSpace(info.Role) ? "Participant" : info.Role!);
        session.SetString(NameKey, info.Name ?? fallbackName ?? info.Email ?? "Runner");
        if (info.ExpiresAt.HasValue)
        {
            session.SetString(ExpiryKey, info.ExpiresAt.Value.ToUnixTimeSeconds().ToString());
        }
    }

    public void UpdateDisplayName(string name) => Session?.SetString(NameKey, name);

    public void SignOut() => Session?.Clear();

    private bool IsExpired()
    {
        var raw = Session?.GetString(ExpiryKey);
        return long.TryParse(raw, out var seconds) &&
               DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
    }
}
