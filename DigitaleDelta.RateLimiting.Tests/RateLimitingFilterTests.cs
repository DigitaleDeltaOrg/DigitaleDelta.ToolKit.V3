using System.Net;
using System.Security.Claims;

namespace DigitaleDelta.RateLimiting.Tests;

using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RateLimiting;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RateLimitingFilterTests
{
    private static RateLimitingFilter CreateFilter(int limit = 3, int concurrentLimit = 2)
    {
        var options = new RateLimitOptions { Limit = limit, Unit = "m", NumberOfConcurrentRequests = concurrentLimit };
        var config = new Dictionary<string, RateLimitOptions>
        {
            { "TestController", options }
        };
        return new RateLimitingFilter(Options.Create(config));
    }

    private static ActionExecutingContext CreateContext(string controllerName, string identity)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["identity"] = identity;
        // Simuleer de identity zoals jouw filter die verwacht:
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
                { new Claim("sub", identity) })
        );
        var controller = new object();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller
        );
    }

    [Fact]
    public async Task OnActionExecutionAsync_AllowedRequest_DoesNotSetContentResult()
    {
        // Arrange
        var filter = CreateFilter(limit: 3, concurrentLimit: 2);
        var userId = "user-1";

        var context = CreateContext("TestController", userId);
        context.HttpContext.Session = new TestSession(userId);
        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback; // BELANGRIJK

        // Zorg dat je user claim overeenkomt met wat jouw filter zoekt, bijvoorbeeld:
        context.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("sub", userId) }, "TestAuth")
        );

        var nextCalled = false;
        var next = new ActionExecutionDelegate(() =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_SeparateIdentities_DontCountAgainstEachOther()
    {
        // Arrange
        var filter = CreateFilter(limit: 1, concurrentLimit: 1);
        var contextA = CreateContext("TestController", "user-A");
        contextA.HttpContext.Session = new TestSession("sessA");
        contextA.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        contextA.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-A") }));

        var contextB = CreateContext("TestController", "user-B");
        contextB.HttpContext.Session = new TestSession("sessB");
        contextB.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        contextB.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-B") }));

        var next = new ActionExecutionDelegate(() => Task.FromResult<ActionExecutedContext>(null!));

        await filter.OnActionExecutionAsync(contextA, next); // user-A: toegestaan
        await filter.OnActionExecutionAsync(contextB, next); // user-B: toegestaan

        // Assert
        Assert.Null(contextA.Result);
        Assert.Null(contextB.Result);
    }

    private class TestSession : ISession
    {
        public string Id { get; }
        public TestSession(string id) => Id = id;
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task LoadAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Remove(string key) => throw new NotImplementedException();
        public void Set(string key, byte[] value) => throw new NotImplementedException();

        public bool TryGetValue(string key, out byte[] value)
        {
            value = null;
            return false;
        }
    }
}
