namespace RaceDay.Web.Services;

/// <summary>
/// Every Part 2 API route used by the MVC application lives here. If a route
/// in the Part 2 API differs, change it in this one file.
/// </summary>
public static class ApiRoutes
{
    public const string Login = "api/auth/login";
    public const string Register = "api/auth/register";
    public const string Me = "api/users/me";
    public const string MyPicture = "api/users/me/picture";

    public const string Events = "api/events";
    public static string Event(int id) => $"api/events/{id}";
    public static string EventBanner(int id) => $"api/events/{id}/banner";
    public static string EventCategories(int id) => $"api/events/{id}/categories";
    public static string EventEnrolments(int id) => $"api/events/{id}/enrolments";
    public static string EventResults(int id) => $"api/events/{id}/results";

    public const string Categories = "api/categories";

    public const string Enrolments = "api/enrolments";
    public const string MyEnrolments = "api/enrolments/my";
    public static string Enrolment(int id) => $"api/enrolments/{id}";

    public const string Results = "api/results";
    public const string MyResults = "api/results/my";
    public static string Result(int id) => $"api/results/{id}";
}
