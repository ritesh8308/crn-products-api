using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Seeds a default Admin account on startup if none exists. Needed because
/// self-registration only ever creates plain Users — without a seeded Admin there
/// would be no way to exercise the Admin-only endpoints. DEV CONVENIENCE ONLY:
/// in a real deployment these credentials would come from a secret store, and the
/// password would be rotated. Documented in the README for the assessment demo.
/// </summary>
public static class IdentitySeeder
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@123";

    public static async Task SeedAdminAsync(
        ApplicationDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        var adminExists = await context.Users.AnyAsync(u => u.Role == Roles.Admin, cancellationToken);
        if (adminExists)
            return;

        context.Users.Add(new User
        {
            Username = DefaultAdminUsername,
            PasswordHash = passwordHasher.Hash(DefaultAdminPassword),
            Role = Roles.Admin,
            CreatedOn = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
