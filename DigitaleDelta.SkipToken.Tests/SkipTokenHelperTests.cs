using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DigitaleDelta.SkipToken.Tests;

public class SkipTokenHelperTests
{
    // Deze key komt overeen met die in SkipTokenHelper
    private static readonly byte[] HmacKey = "JOUW-ZEER-GEHEIME-SLEUTEL-HIER"u8.ToArray();
    private static SkipTokenHelper SkipTokenHelper { get; } = new("JOUW-ZEER-GEHEIME-SLEUTEL-HIER");

    private static string BuildTokenQueryParam(DateTimeOffset creationDate, string requestUrl = "data?x=1", string lastId = "42")
    {
        var skipToken = new SkipToken { RequestUrl = requestUrl, LastId = lastId, CreationDate = creationDate };
        var skipTokenJson = System.Text.Json.JsonSerializer.Serialize(skipToken);
        var jsonBytes = Encoding.UTF8.GetBytes(skipTokenJson);
        var base64Json = Convert.ToBase64String(jsonBytes);
        using var hmac = new HMACSHA256(HmacKey);
        var hmacHash = hmac.ComputeHash(jsonBytes);
        var base64Hmac = Convert.ToBase64String(hmacHash);
        var fullToken = $"{base64Json}.{base64Hmac}";

        return $"$skiptoken={WebUtility.UrlEncode(fullToken)}";
    }

    [Fact]
    public void ExtractFromUrl_ShouldReturnNullAndInvalid_WhenUrlIsNullOrEmpty()
    {
        var valid = SkipTokenHelper.TryExtractFromUrl(null, out var token, out var error);
        Assert.Null(token);
        Assert.False(valid);
        Assert.NotNull(error);

        valid = SkipTokenHelper.TryExtractFromUrl("", out var token2, out error);
        Assert.Null(token2);
        Assert.False(valid);
    }

    [Fact]
    public void ExtractFromUrl_ShouldReturnNullAndInvalid_WhenNoSkipTokenPresent()
    {
        var url = "https://test.com/data?foo=1";
        var valid = SkipTokenHelper.TryExtractFromUrl(url, out var token, out var error);
        Assert.Null(token);
        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void ExtractFromUrl_ShouldReturnValidTokenAndValidFalse_WhenTokenJustCreated()
    {
        var creation = DateTimeOffset.Now;
        var param = BuildTokenQueryParam(creation);
        var url = $"https://test.com/data?{param}";
        var valid = SkipTokenHelper.TryExtractFromUrl(url, out var token, out var error, 10);

        Assert.NotNull(token);
        Assert.Equal("42", token.LastId);
        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void ExtractFromUrl_ShouldReturnValidTokenAndTrue_WhenTokenExpired()
    {
        var creation = DateTimeOffset.Now.AddMinutes(-20);
        var param = BuildTokenQueryParam(creation);
        var url = $"https://api.something/?foo=a&{param}&bar=b";
        var valid = SkipTokenHelper.TryExtractFromUrl(url, out var token, out var error, 10);

        Assert.NotNull(token);
        Assert.Equal("data?x=1", token.RequestUrl);
        Assert.False(valid);
        Assert.Equal("The skip token has expired.", error);
    }

    [Fact]
    public void ExtractFromUrl_ShouldReturnNullAndInvalid_WhenSkipTokenTampered()
    {
        // Ongeldige (niet-kloppende) HMAC!
        var skipToken = new SkipToken { RequestUrl = "abc", LastId = "x", CreationDate = DateTimeOffset.Now };
        var skipTokenJson = System.Text.Json.JsonSerializer.Serialize(skipToken);
        var jsonBytes = Encoding.UTF8.GetBytes(skipTokenJson);
        var base64Json = Convert.ToBase64String(jsonBytes);

        // Maak een random (onjuiste) HMAC
        var fakeHmac = Convert.ToBase64String(new byte[32]);
        var param = $"{base64Json}.{fakeHmac}";
        var url = $"https://test.com/data?$skiptoken={WebUtility.UrlEncode(param)}";

        var valid = SkipTokenHelper.TryExtractFromUrl(url, out var token, out var error, 10);
        Assert.Null(token);
        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ExtractFromUrl_ShouldNotWorkWithRelativeUrl()
    {
        var creation = DateTimeOffset.UtcNow;
        var param = BuildTokenQueryParam(creation, "/apitest", "77");
        var url = $"/somecontroller?foo=bar&{param}";
        var valid = SkipTokenHelper.TryExtractFromUrl(url, out var token, out var error);

        Assert.Null(token);
        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ConstructFromUrl_ReturnsValidSkipToken()
    {
        var baseUrl = "https://host/api/data?$top=10&$skiptoken=verwijderen&foo=bar";
        var lastId = "123";
        var valid = SkipTokenHelper.TryConstructFromUrl(baseUrl, lastId, out var tokenString);

        Assert.False(string.IsNullOrWhiteSpace(tokenString));
        var parts = tokenString.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.True(valid);

        // Het resultaat van ConstructFromUrl is niet URL-encoded - dat gebeurt pas bij het in query plaatsen
        // Simuleer een request:
        var url = $"https://host/api/data?$skiptoken={WebUtility.UrlEncode(tokenString)}";
        valid = SkipTokenHelper.TryExtractFromUrl(url, out var token, out var error, 10);

        Assert.NotNull(token);
        Assert.Equal(lastId, token.LastId);
        Assert.Contains("foo=bar", token.RequestUrl); // RequestUrl bevat enkel de resterende query zonder oude $skiptoken
        Assert.True(valid); // net aangemaakt = niet expired
        Assert.Null(error);
    }

    [Fact]
    public void ConstructFromUrl_EmptyBaseUrl_ReturnsNull()
    {
        var valid = SkipTokenHelper.TryConstructFromUrl("", "X", out var tokenString);
        Assert.Equal("", tokenString);
        Assert.False(valid);

        valid = SkipTokenHelper.TryConstructFromUrl(null, "X", out tokenString);
        Assert.Null(tokenString);
        Assert.False(valid);
    }

    [Fact]
    public void ConstructFromUrl_RemovesExistingSkiptokenFromQuery()
    {
        var url = "https://api/data?foo=a&$skiptoken=blabla&bar=b";
        var lastId = "zzz";
        var valid = SkipTokenHelper.TryConstructFromUrl(url, lastId, out var tokenString);
        Assert.True(valid);

        var decoded = WebUtility.UrlEncode(tokenString);
        var testUrl = $"https://api/data?foo=a&$skiptoken={decoded}&bar=b";
        valid = SkipTokenHelper.TryExtractFromUrl(testUrl, out var token, out var error);

        Assert.NotNull(token);
        Assert.DoesNotContain("$skiptoken", token.RequestUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blabla", token.RequestUrl);
        Assert.Equal(lastId, token.LastId);
        Assert.True(valid);
        Assert.Null(error);
    }
}
