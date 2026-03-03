using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.Authentication.Tests;

public class NoAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsAnonymousPrincipal_WithSessionIdClaim()
    {
        // Arrange
        var handler = new NoAuthenticationHandler();
        var context = new DefaultHttpContext
        {
            Session = new TestSession("sess-123")
        };

        // Act
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        // Assert
        Assert.True(authenticated);
        Assert.NotNull(principal);
        Assert.Equal("None", principal.Identity?.AuthenticationType);
        Assert.Equal("true", principal.FindFirst("anonymous")?.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_GeneratesSessionId_IfSessionIsNull()
    {
        // Arrange
        var handler = new NoAuthenticationHandler();
        var context = new DefaultHttpContext
        {
            Session = null
        };

        // Act
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        // Assert
        Assert.True(authenticated);
        Assert.NotNull(principal);
        Assert.Equal("None", principal.Identity?.AuthenticationType);
        Assert.Equal("true", principal.FindFirst("anonymous")?.Value);
        Assert.False(string.IsNullOrEmpty(principal.FindFirst("anon.sessionid")?.Value));
    }

    // Fake session for testing
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
        public bool TryGetValue(string key, out byte[] value) { value = null; return false; }
    }
}
