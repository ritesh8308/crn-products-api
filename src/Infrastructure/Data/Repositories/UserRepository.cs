using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository. Unlike the product read paths, these
/// queries TRACK the entities they return: login appends a refresh token to the user,
/// and refresh mutates/rotates tokens — so EF must watch them for SaveChangesAsync.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token, CancellationToken cancellationToken = default)
    {
        // Load the token's user AND all of that user's tokens, so the service can
        // rotate this one and revoke the whole family on replay without extra queries.
        return await _context.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u!.RefreshTokens)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }
}
