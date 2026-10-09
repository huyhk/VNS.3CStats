using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace VNS.ThreeCStats.Infrastructure.Identity;

public interface IEmergencyLoginService
{
    bool IsAvailable { get; }
    ClaimsPrincipal? Authenticate(string? username, string? password);
}

public sealed class EmergencyLoginService(IOptions<EmergencyLoginOptions> options)
    : IEmergencyLoginService
{
    public const string AuthenticationType = "EmergencyLogin";
    public const string EmergencyClaimType = "vns:emergency-login";

    private readonly EmergencyLoginOptions _options = options.Value;

    public bool IsAvailable =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.Username) &&
        !string.IsNullOrEmpty(_options.Password);

    public ClaimsPrincipal? Authenticate(string? username, string? password)
    {
        if (!IsAvailable || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        if (!FixedTimeEquals(username, _options.Username!) ||
            !FixedTimeEquals(password, _options.Password!))
        {
            return null;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "emergency-superadmin"),
            new Claim(ClaimTypes.Name, _options.Username!),
            new Claim(ClaimTypes.Role, AppRoles.SuperAdmin),
            new Claim(EmergencyClaimType, bool.TrueString)
        };

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, AuthenticationType));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightBytes = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
