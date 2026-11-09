using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public static class ApiMapper
{
    public static EventItem ToEvent(JsonElement el)
    {
        var item = new EventItem
        {
            Id = Js.Int(el, "id", "eventId") ?? 0,
            Name = Js.Str(el, "name", "title", "eventName") ?? "",
            Description = Js.Str(el, "description", "details") ?? "",
            Location = Js.Str(el, "location", "venue") ?? "",
            Date = Js.Date(el, "eventDate", "date", "startDate", "startDateTime") ?? default,
            DistanceKm = Js.Dec(el, "distanceKm", "distance") ?? 0,
            EventType = Js.Str(el, "eventType", "type") ?? "",
            BannerUrl = Js.Str(el, "bannerImageUrl", "bannerUrl", "bannerImage", "imageUrl"),
            OrganiserId = Js.Int(el, "organiserId", "organizerId", "createdBy") ?? 0
        };

        var cats = Js.Prop(el, "categories", "eventCategories");
        if (cats is { ValueKind: JsonValueKind.Array })
        {
            item.Categories = cats.Value.EnumerateArray().Select(ToCategory).ToList();
        }
        return item;
    }

    public static CategoryItem ToCategory(JsonElement el)
    {
        var inner = Js.Prop(el, "category");
        var source = inner is { ValueKind: JsonValueKind.Object } ? inner.Value : el;

        var categoryId = Js.Int(source, "categoryId", "id") ?? Js.Int(el, "categoryId", "id") ?? 0;
        var eventCategoryId = Js.Int(el, "eventCategoryId") ?? Js.Int(el, "id") ?? categoryId;

        return new CategoryItem
        {
            Id = categoryId,
            EventCategoryId = eventCategoryId,
            Name = Js.Str(source, "name", "categoryName", "title") ?? Js.Str(el, "categoryName", "name") ?? "",
            Description = Js.Str(source, "description") ?? "",
            CategoryType = Js.Str(source, "categoryType", "type") ?? "",
            MinAge = Js.Int(source, "minAge", "minimumAge"),
            MaxAge = Js.Int(source, "maxAge", "maximumAge"),
            DistanceKm = Js.Dec(source, "distanceKm", "distance")
        };
    }

    public static EnrolmentItem ToEnrolment(JsonElement el)
    {
        var ev = Js.Prop(el, "event");
        var cat = Js.Prop(el, "category", "eventCategory");
        var user = Js.Prop(el, "user", "participant");

        string? participant = Js.Str(el, "participantName", "userName", "fullName");
        if (participant is null && user is { ValueKind: JsonValueKind.Object })
        {
            var first = Js.Str(user.Value, "firstName") ?? "";
            var last = Js.Str(user.Value, "lastName") ?? "";
            participant = $"{first} {last}".Trim();
            if (participant.Length == 0) participant = Js.Str(user.Value, "name", "email");
        }

        return new EnrolmentItem
        {
            Id = Js.Int(el, "id", "enrolmentId") ?? 0,
            EventId = Js.Int(el, "eventId") ??
                      (ev is { ValueKind: JsonValueKind.Object } ? Js.Int(ev.Value, "id", "eventId") : null) ?? 0,
            EventName = Js.Str(el, "eventName") ??
                        (ev is { ValueKind: JsonValueKind.Object } ? Js.Str(ev.Value, "name", "title") : null) ?? "",
            EventDate = Js.Date(el, "eventDate") ??
                        (ev is { ValueKind: JsonValueKind.Object } ? Js.Date(ev.Value, "eventDate", "date") : null),
            CategoryId = Js.Int(el, "eventCategoryId", "categoryId") ?? 0,
            CategoryName = Js.Str(el, "categoryName") ??
                           (cat is { ValueKind: JsonValueKind.Object }
                               ? Js.Str(cat.Value, "name", "categoryName") : null) ?? "",
            ParticipantName = participant ?? "",
            Status = Js.Str(el, "status", "enrolmentStatus") ?? "Pending",
            EnrolledAt = Js.Date(el, "enrolledAt", "enrolmentDate", "createdAt", "registeredAt")
        };
    }

    public static ResultItem ToResult(JsonElement el)
    {
        var enrol = Js.Prop(el, "enrolment");
        var ev = Js.Prop(el, "event");

        string? FromEnrol(Func<JsonElement, string?> f) =>
            enrol is { ValueKind: JsonValueKind.Object } ? f(enrol.Value) : null;

        var time = Js.Str(el, "finishTime", "finishingTime", "time") ?? "";
        if (time.Contains('.') && time.Length > 8) time = time[..8];

        return new ResultItem
        {
            Id = Js.Int(el, "id", "resultId") ?? 0,
            EnrolmentId = Js.Int(el, "enrolmentId") ??
                          (enrol is { ValueKind: JsonValueKind.Object } ? Js.Int(enrol.Value, "id") : null) ?? 0,
            EventId = Js.Int(el, "eventId") ??
                      (ev is { ValueKind: JsonValueKind.Object } ? Js.Int(ev.Value, "id", "eventId") : null) ??
                      (enrol is { ValueKind: JsonValueKind.Object } ? Js.Int(enrol.Value, "eventId") : null) ?? 0,
            EventName = Js.Str(el, "eventName") ??
                        (ev is { ValueKind: JsonValueKind.Object } ? Js.Str(ev.Value, "name", "title") : null) ??
                        FromEnrol(e => Js.Str(e, "eventName")) ?? "",
            EventDate = Js.Date(el, "eventDate") ??
                        (ev is { ValueKind: JsonValueKind.Object } ? Js.Date(ev.Value, "eventDate", "date") : null),
            CategoryName = Js.Str(el, "categoryName") ?? FromEnrol(e => Js.Str(e, "categoryName")) ?? "",
            ParticipantName = Js.Str(el, "participantName", "userName", "fullName") ??
                              FromEnrol(e => Js.Str(e, "participantName", "userName")) ?? "",
            FinishTime = time,
            Position = Js.Int(el, "position", "finishPosition", "finishingPosition", "rank") ?? 0,
            TotalFinishers = Js.Int(el, "totalFinishers", "totalParticipants", "totalEntrants")
        };
    }

    public static UserProfile ToProfile(JsonElement el)
    {
        var profile = new UserProfile
        {
            FirstName = Js.Str(el, "firstName", "givenName") ?? "",
            LastName = Js.Str(el, "lastName", "surname", "familyName") ?? "",
            Email = Js.Str(el, "email") ?? "",
            Phone = Js.Str(el, "phoneNumber", "phone", "cellNumber") ?? "",
            ProfileImageUrl = Js.Str(el, "profileImageUrl", "profilePictureUrl", "pictureUrl", "avatarUrl")
        };

        if (profile.FirstName.Length == 0 && profile.LastName.Length == 0)
        {
            var full = Js.Str(el, "fullName", "name") ?? "";
            var idx = full.IndexOf(' ');
            if (idx > 0)
            {
                profile.FirstName = full[..idx];
                profile.LastName = full[(idx + 1)..];
            }
            else
            {
                profile.FirstName = full;
            }
        }
        return profile;
    }
}
