namespace VNS.ThreeCStats.Infrastructure.Identity;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool AllowRegistration { get; set; }
    public bool AllowLocalLogin { get; set; } = true;
    public bool AllowGoogleLogin { get; set; }
    public bool AllowMicrosoftLogin { get; set; }
}

public sealed class EmergencyLoginOptions
{
    public const string SectionName = "EmergencyLogin";

    public bool Enabled { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
