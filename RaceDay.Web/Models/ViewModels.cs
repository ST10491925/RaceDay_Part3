namespace RaceDay.Web.Models;

public class HomeViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public decimal? MaxDistance { get; set; }
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Type) ||
        !string.IsNullOrWhiteSpace(Category) || MaxDistance.HasValue;
}

public class EventDetailViewModel
{
    public EventItem Event { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public EnrolmentItem? MyEnrolment { get; set; }
}

public class DashboardViewModel
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public List<DashboardRow> Rows { get; set; } = new();
    public List<DashboardRow> Upcoming => Rows.Where(r => r.Event.IsUpcoming)
        .OrderBy(r => r.Event.Date).ToList();
}

public class DashboardRow
{
    public EventItem Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class ProfilePageViewModel
{
    public ProfileForm Form { get; set; } = new();
}

public class CategoryManagementViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<CategoryItem> AllCategories { get; set; } = new();
    public List<CategoryItem> EventCategories { get; set; } = new();
    public CategoryForm NewCategory { get; set; } = new();
}

public class EnrolmentManagementViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<EnrolmentItem> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentItem Enrolment { get; set; } = new();
    public ResultItem? Result { get; set; }
}

public class ResultManagementViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public EventItem? SelectedEvent { get; set; }
    public List<ResultRow> Rows { get; set; } = new();
    public bool Concluded => SelectedEvent is not null && SelectedEvent.Date <= DateTime.Now;
}

public class ErrorViewModel
{
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Try again, or return to the home page.";
    public int? Code { get; set; }
}
