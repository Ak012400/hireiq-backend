namespace HireIQ.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "HireIQ";
    public string Audience { get; set; } = "HireIQ.Client";
    public int AccessTokenHours { get; set; } = 24;
}
