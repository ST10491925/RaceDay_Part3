using System.Text;
using Microsoft.AspNetCore.Http;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;

namespace RaceDay.Web.Tests;

public class HelperTests
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(47, "47th")]
    [InlineData(112, "112th")]
    public void Ordinal_formats_positions(int n, string expected) =>
        Assert.Equal(expected, Format.Ordinal(n));

    [Theory]
    [InlineData("Confirmed", "status-confirmed")]
    [InlineData("confirmed", "status-confirmed")]
    [InlineData("Pending", "status-pending")]
    [InlineData("Cancelled", "status-cancelled")]
    [InlineData("Something else", "status-neutral")]
    [InlineData(null, "status-neutral")]
    public void Status_colour_classes(string? status, string expected) =>
        Assert.Equal(expected, Format.StatusClass(status));

    [Theory]
    [InlineData("01:42:09", true)]
    [InlineData("1:05:00", true)]
    [InlineData("00:59:59", true)]
    [InlineData("01:60:00", false)]
    [InlineData("1h42m", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Finish_time_validation(string? value, bool expected) =>
        Assert.Equal(expected, ResultForm.IsValidFinishTime(value));

    private static IFormFile File(string contentType, long length)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("x"));
        return new FormFile(stream, 0, length, "file", "banner.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public void Valid_image_passes() => Assert.Null(Format.ValidateImage(File("image/png", 1024)));

    [Fact]
    public void Non_image_is_rejected() => Assert.NotNull(Format.ValidateImage(File("application/pdf", 1024)));

    [Fact]
    public void Oversized_image_is_rejected() =>
        Assert.NotNull(Format.ValidateImage(File("image/jpeg", Format.MaxImageBytes + 1)));

    [Fact]
    public void Age_category_label_includes_range() =>
        Assert.Equal("Junior (12 to 17 years)",
            Format.CategoryLabel(new CategoryItem { Name = "Junior", MinAge = 12, MaxAge = 17 }));

    [Fact]
    public void Distance_category_label_includes_distance() =>
        Assert.Equal("Half (21.1 km)",
            Format.CategoryLabel(new CategoryItem { Name = "Half", DistanceKm = 21.1m }));
}
