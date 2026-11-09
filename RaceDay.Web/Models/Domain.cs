namespace RaceDay.Web.Models;

public class EventItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
    public int OrganiserId { get; set; }
    public List<CategoryItem> Categories { get; set; } = new();
    public bool IsUpcoming => Date >= DateTime.Now;
}

public class CategoryItem
{
    public int Id { get; set; }
    public int EventCategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string CategoryType { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class EnrolmentItem
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime? EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string ParticipantName { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime? EnrolledAt { get; set; }
}

public class ResultItem
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime? EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string ParticipantName { get; set; } = "";
    public string FinishTime { get; set; } = "";
    public int Position { get; set; }
    public int? TotalFinishers { get; set; }
}

public class UserProfile
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}
