using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.Authentication.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsPrincipal_WhenApiKeyIsPresent()
    {
        // Arrange
        var handler = new ApiKeyAuthenticationHandler("X-API-KEY");
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-KEY"] = "dummy-password-for-unit-test";

        // Act
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("APIKEY", principal.Identity?.AuthenticationType);
        Assert.Equal("dummy-password-for-unit-test", principal.FindFirst("api_key")?.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenApiKeyIsMissing()
    {
        // Arrange
        var handler = new ApiKeyAuthenticationHandler("X-API-KEY");
        var context = new DefaultHttpContext();

        // Act
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        // Assert
        Assert.Null(principal);
    }
}
