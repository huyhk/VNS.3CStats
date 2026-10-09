using System.Security.Claims;
using Microsoft.Extensions.Options;
using VNS.ThreeCStats.Infrastructure.Identity;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Identity;

public sealed class EmergencyLoginServiceTests
{
    [Fact]
    public void IsAvailable_IsFalse_WhenCredentialsAreMissing()
    {
        var service = CreateService(new EmergencyLoginOptions
        {
            Enabled = true
        });

        Assert.False(service.IsAvailable);
        Assert.Null(service.Authenticate("admin", "secret"));
    }

    [Fact]
    public void Authenticate_ReturnsNull_WhenCredentialsAreWrong()
    {
        var service = CreateService(EnabledOptions());

        Assert.Null(service.Authenticate("admin", "wrong"));
        Assert.Null(service.Authenticate("wrong", "secret"));
    }

    [Fact]
    public void Authenticate_CreatesEmergencySuperAdminPrincipal_WhenCredentialsMatch()
    {
        var service = CreateService(EnabledOptions());

        var principal = service.Authenticate("admin", "secret");

        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal(EmergencyLoginService.AuthenticationType, principal.Identity?.AuthenticationType);
        Assert.True(principal.IsInRole(AppRoles.SuperAdmin));
        Assert.Equal(
            bool.TrueString,
            principal.FindFirstValue(EmergencyLoginService.EmergencyClaimType));
    }

    private static EmergencyLoginService CreateService(EmergencyLoginOptions options) =>
        new(Options.Create(options));

    private static EmergencyLoginOptions EnabledOptions() => new()
    {
        Enabled = true,
        Username = "admin",
        Password = "secret"
    };
}
