using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RaceDay.Web.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterForm
{
    [Required(ErrorMessage = "Enter your first name.")]
    [StringLength(60)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Enter your last name.")]
    [StringLength(60)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Choose a password.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Repeat your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class EventForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter the event name.")]
    [StringLength(120)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Describe the event.")]
    [StringLength(2000)]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Enter the location.")]
    [StringLength(200)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Choose the event date and start time.")]
    [DataType(DataType.DateTime)]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Enter the distance in kilometres.")]
    [Range(0.1, 500, ErrorMessage = "Distance must be between 0.1 and 500 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? CurrentBannerUrl { get; set; }
}

public class CategoryForm
{
    [Required(ErrorMessage = "Enter a category name.")]
    [StringLength(80)]
    public string Name { get; set; } = "";

    [StringLength(300)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose a category type.")]
    [Display(Name = "Category type")]
    public string CategoryType { get; set; } = "Age";

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.1, 500, ErrorMessage = "Distance must be between 0.1 and 500 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

public class ResultForm
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [Required(ErrorMessage = "Enter the finish time as hh:mm:ss.")]
    [Display(Name = "Finish time")]
    public string FinishTime { get; set; } = "";

    [Required(ErrorMessage = "Enter the finishing position.")]
    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }

    private static readonly Regex TimePattern =
        new(@"^\d{1,2}:[0-5]\d:[0-5]\d$", RegexOptions.Compiled);

    public static bool IsValidFinishTime(string? value) =>
        !string.IsNullOrWhiteSpace(value) && TimePattern.IsMatch(value.Trim());
}

public class ProfileForm
{
    [Required(ErrorMessage = "Enter your first name.")]
    [StringLength(60)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Enter your last name.")]
    [StringLength(60)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    public string Email { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
}
