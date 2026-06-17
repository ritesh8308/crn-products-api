namespace Domain.Constants;

/// <summary>
/// The fixed set of authorization roles. Centralized as constants so the role
/// string is written once and reused by the token generator (claims), the
/// seeder, and the controllers' [Authorize(Roles = ...)] attributes — no
/// stringly-typed "Admin" scattered around to typo.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
