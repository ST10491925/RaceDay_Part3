using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

public class SessionAuthorizeTests
{
    private static AuthorizationFilterContext Context(FakeSession session)
    {
        var services = new ServiceCollection().AddSingleton<IUserSession>(session).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        return new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
    }

    [Fact]
    public void Unauthenticated_user_is_redirected_to_login()
    {
        var ctx = Context(new FakeSession());

        new SessionAuthorizeAttribute { Role = "Organiser" }.OnAuthorization(ctx);

        var redirect = Assert.IsType<RedirectToActionResult>(ctx.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
    }

    [Fact]
    public void Participant_cannot_open_organiser_pages()
    {
        var ctx = Context(new FakeSession { Token = "t", Role = "Participant" });

        new SessionAuthorizeAttribute { Role = "Organiser" }.OnAuthorization(ctx);

        var redirect = Assert.IsType<RedirectToActionResult>(ctx.Result);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void Organiser_cannot_open_participant_pages()
    {
        var ctx = Context(new FakeSession { Token = "t", Role = "Organiser" });

        new SessionAuthorizeAttribute { Role = "Participant" }.OnAuthorization(ctx);

        var redirect = Assert.IsType<RedirectToActionResult>(ctx.Result);
        Assert.Equal("Dashboard", redirect.ActionName);
    }

    [Fact]
    public void Matching_role_is_allowed_through()
    {
        var ctx = Context(new FakeSession { Token = "t", Role = "Organiser" });

        new SessionAuthorizeAttribute { Role = "Organiser" }.OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void Any_signed_in_user_passes_when_no_role_is_required()
    {
        var ctx = Context(new FakeSession { Token = "t", Role = "Participant" });

        new SessionAuthorizeAttribute().OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }
}
