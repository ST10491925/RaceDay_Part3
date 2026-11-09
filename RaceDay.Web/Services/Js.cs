using System.Globalization;
using System.Text.Json;

namespace RaceDay.Web.Services;

/// <summary>
/// Tolerant JSON readers. Property names are matched case-insensitively and
/// several alternative names can be supplied, so small naming differences in the
/// Part 2 API responses do not break the MVC pages.
/// </summary>
public static class Js
{
    public static JsonElement? Prop(JsonElement el, params string[] names)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in names)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (p.Value.ValueKind == JsonValueKind.Null) continue;
                    return p.Value;
                }
            }
        }
        return null;
    }

    public static string? Str(JsonElement el, params string[] names)
    {
        var v = Prop(el, names);
        if (v is null) return null;
        return v.Value.ValueKind == JsonValueKind.String ? v.Value.GetString() : v.Value.ToString();
    }

    public static int? Int(JsonElement el, params string[] names)
    {
        var v = Prop(el, names);
        if (v is null) return null;
        if (v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetInt32(out var i)) return i;
        if (v.Value.ValueKind == JsonValueKind.String &&
            int.TryParse(v.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)) return s;
        return null;
    }

    public static decimal? Dec(JsonElement el, params string[] names)
    {
        var v = Prop(el, names);
        if (v is null) return null;
        if (v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetDecimal(out var d)) return d;
        if (v.Value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(v.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
        return null;
    }

    public static DateTime? Date(JsonElement el, params string[] names)
    {
        var v = Prop(el, names);
        if (v is null) return null;
        if (v.Value.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(v.Value.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dt))
        {
            // Values with a Z are UTC and are shown in local time; values without a zone are used as written.
            return dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        }
        return null;
    }

    public static IEnumerable<JsonElement> Items(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray().ToList();
        if (root.ValueKind != JsonValueKind.Object) return Enumerable.Empty<JsonElement>();

        var wrapped = Prop(root, "data", "items", "results", "value");
        if (wrapped is { ValueKind: JsonValueKind.Array }) return wrapped.Value.EnumerateArray().ToList();

        foreach (var p in root.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.Array) return p.Value.EnumerateArray().ToList();
        }
        return Enumerable.Empty<JsonElement>();
    }
}
