using System.Globalization;

namespace RaceDay.Web.Helpers;

public static class Format
{
    public static string Ordinal(int n)
    {
        var abs = Math.Abs(n);
        var suffix = (abs % 100) is 11 or 12 or 13
            ? "th"
            : (abs % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    public static string StatusClass(string? status) =>
        (status ?? "").Trim().ToLowerInvariant() switch
        {
            "confirmed" => "status-confirmed",
            "pending" => "status-pending",
            "cancelled" or "canceled" => "status-cancelled",
            "completed" => "status-completed",
            _ => "status-neutral"
        };

    public static string StatusLabel(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();

    public static string Date(DateTime? d) => d.HasValue && d.Value.Year > 1 ? d.Value.ToString("ddd d MMM yyyy") : "Date to be confirmed";
    public static string DateTimeFull(DateTime d) => d.Year > 1 ? d.ToString("dddd d MMMM yyyy, HH:mm") : "Date to be confirmed";
    public static string Distance(decimal km) =>
        km % 1 == 0
            ? km.ToString("0", CultureInfo.InvariantCulture) + " km"
            : km.ToString("0.0#", CultureInfo.InvariantCulture) + " km";

    public static string CategoryLabel(Models.CategoryItem c)
    {
        if (c.MinAge.HasValue || c.MaxAge.HasValue)
        {
            var range = c.MinAge.HasValue && c.MaxAge.HasValue ? $"{c.MinAge} to {c.MaxAge} years"
                      : c.MinAge.HasValue ? $"{c.MinAge} years and over"
                      : $"up to {c.MaxAge} years";
            return $"{c.Name} ({range})";
        }
        if (c.DistanceKm.HasValue) return $"{c.Name} ({Distance(c.DistanceKm.Value)})";
        return c.Name;
    }

    public static readonly string[] EventTypes = { "Run", "Walk" };

    public static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
    public const long MaxImageBytes = 5 * 1024 * 1024;

    public static string? ValidateImage(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        if (!AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return "Upload a JPG, PNG or WebP image.";
        if (file.Length > MaxImageBytes)
            return "The image is larger than 5 MB. Choose a smaller file.";
        return null;
    }
}
