using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace DigitaleDelta.Authentication.Tests;

public class JwtAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsPrincipal_WhenJwtValid()
    {
        // Arrange
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsAtLeast32Characters_LongAndSecure!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Name, "John")],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials
        ));
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false
        };
        var handler = new JwtAuthenticationHandler(validationParameters);
        var context = new DefaultHttpContext();

        context.Request.Headers["Authorization"] = "Bearer " + token;

        // Act
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        // Assert
        Assert.True(authenticated);
        Assert.NotNull(principal);
        Assert.Equal("John", principal.Identity?.Name);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenNoAuthorizationHeader()
    {
        var validationParameters = new TokenValidationParameters();
        var handler = new JwtAuthenticationHandler(validationParameters);
        var context = new DefaultHttpContext();
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        Assert.False(authenticated);
        Assert.Null(principal);
    }
}