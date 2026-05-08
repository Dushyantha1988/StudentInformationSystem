using Microsoft.Extensions.Options;
using StudentInformationSystem.Models;
using StudentInformationSystem.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace StudentInformationSystem.Tests;

public class JwtServiceTests
{
    [Fact]
    public void GenerateTokens_ReturnsAccessAndRefreshTokensWithExpectedMetadata()
    {
        var service = CreateService();
        var user = new ApplicationUser { UserName = "alice@example.com" };

        var before = DateTime.UtcNow;
        var response = service.GenerateTokens(user);
        var after = DateTime.UtcNow;

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.True(response.ExpiresDate > before.ToLocalTime().AddMinutes(10));
        Assert.True(response.ExpiresDate < after.ToLocalTime().AddMinutes(30));
    }

    [Fact]
    public void GenerateTokens_AccessTokenContainsUserNameClaimAndIssuer()
    {
        var service = CreateService();
        var user = new ApplicationUser { UserName = "alice@example.com" };

        var response = service.GenerateTokens(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Name && c.Value == "alice@example.com");
    }

    private static JwtService CreateService()
    {
        var appSettings = Options.Create(new AppSettings
        {
            Key = "this-is-a-long-test-signing-key-123456",
            Issuer = "test-issuer",
            AccessTokenExpirationMinutes = 20
        });
        return new JwtService(appSettings);
    }
}
