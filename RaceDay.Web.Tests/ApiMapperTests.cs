using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

public class ApiMapperTests
{
    [Fact]
    public void Event_mapping_accepts_alternative_property_names()
    {
        var el = TestFactory.Parse("""
            {"eventId":7,"title":"Dusi 10K","date":"2026-12-01T05:00:00Z","distance":10,
             "type":"Run","bannerUrl":"https://blob.test/a.jpg","organizerId":3}
            """);

        var ev = ApiMapper.ToEvent(el);

        Assert.Equal(7, ev.Id);
        Assert.Equal("Dusi 10K", ev.Name);
        Assert.Equal(10m, ev.DistanceKm);
        Assert.Equal("Run", ev.EventType);
        Assert.Equal("https://blob.test/a.jpg", ev.BannerUrl);
        Assert.Equal(3, ev.OrganiserId);
        Assert.Equal(new DateTime(2026, 12, 1, 5, 0, 0, DateTimeKind.Utc), ev.Date.ToUniversalTime());
    }

    [Fact]
    public void Property_names_are_case_insensitive()
    {
        var ev = ApiMapper.ToEvent(TestFactory.Parse("""{"ID":2,"NAME":"Night Walk","LOCATION":"Durban"}"""));
        Assert.Equal(2, ev.Id);
        Assert.Equal("Night Walk", ev.Name);
        Assert.Equal("Durban", ev.Location);
    }

    [Fact]
    public void Lists_are_read_from_bare_arrays_and_wrapped_objects()
    {
        var bare = Js.Items(TestFactory.Parse("""[{"id":1},{"id":2}]""")).ToList();
        var wrapped = Js.Items(TestFactory.Parse("""{"data":[{"id":1},{"id":2},{"id":3}]}""")).ToList();
        var empty = Js.Items(TestFactory.Parse("""{"count":0}""")).ToList();

        Assert.Equal(2, bare.Count);
        Assert.Equal(3, wrapped.Count);
        Assert.Empty(empty);
    }

    [Fact]
    public void Enrolment_mapping_reads_nested_event_category_and_user()
    {
        var el = TestFactory.Parse("""
            {"id":3,"status":"Confirmed","event":{"id":9,"name":"Beach Run"},
             "category":{"name":"Under 20"},"user":{"firstName":"Thandi","lastName":"Zulu"}}
            """);

        var e = ApiMapper.ToEnrolment(el);

        Assert.Equal(3, e.Id);
        Assert.Equal(9, e.EventId);
        Assert.Equal("Beach Run", e.EventName);
        Assert.Equal("Under 20", e.CategoryName);
        Assert.Equal("Thandi Zulu", e.ParticipantName);
        Assert.Equal("Confirmed", e.Status);
    }

    [Fact]
    public void Result_mapping_trims_fractional_seconds_and_reads_position()
    {
        var el = TestFactory.Parse("""
            {"id":4,"enrolmentId":3,"eventId":9,"eventName":"Beach Run","finishTime":"01:42:09.1234567",
             "position":47,"totalFinishers":312}
            """);

        var r = ApiMapper.ToResult(el);

        Assert.Equal("01:42:09", r.FinishTime);
        Assert.Equal(47, r.Position);
        Assert.Equal(312, r.TotalFinishers);
    }

    [Fact]
    public void Category_mapping_reads_nested_category_and_event_category_id()
    {
        var el = TestFactory.Parse("""{"id":11,"category":{"id":2,"name":"Over 50","minAge":50}}""");

        var c = ApiMapper.ToCategory(el);

        Assert.Equal(2, c.Id);
        Assert.Equal(11, c.EventCategoryId);
        Assert.Equal("Over 50", c.Name);
        Assert.Equal(50, c.MinAge);
    }
}
