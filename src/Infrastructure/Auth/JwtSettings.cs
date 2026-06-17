namespace Infrastructure.Auth;

/// <summary>
/// Strongly-typed JWT configuration, bound from the "JwtSettings" section of
/// appsettings. The SAME values are used in two places: the token service (to sign
/// access tokens) and the API's JwtBearer middleware (to validate them) — so issuer,
/// audience and key must match on both sides.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;          // symmetric signing secret (HMAC-SHA256)
    public int AccessTokenMinutes { get; set; } = 15;        // short-lived
    public int RefreshTokenDays { get; set; } = 7;           // long-lived
}
