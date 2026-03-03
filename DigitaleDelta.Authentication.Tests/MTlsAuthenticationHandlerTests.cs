using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.Authentication.Tests;

public class MTlsAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsPrincipal_WhenCertificatePresent()
    {
        // Arrange
        var handler = new MTlsAuthenticationHandler();
        var context = new DefaultHttpContext
        {
            Connection =
            {
                ClientCertificate = CreateSelfSignedCertificate("CN=Test")
            }
        };

        // Act
        var authenticated = await handler.TryAuthenticateAsync(context, out var principal);

        // Assert
        Assert.True(authenticated);
        Assert.NotNull(principal);
        Assert.Equal("mTLS", principal.Identity?.AuthenticationType);
        Assert.Equal("CN=Test", principal.FindFirst("x509.subject")?.Value);
        Assert.NotNull(principal.FindFirst("x509.thumbprint")?.Value);
    }
    
    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName = "CN=Test")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return cert;
    }
}