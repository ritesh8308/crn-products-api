using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Persistence contract for users and their refresh tokens. Implemented in
/// Infrastructure (EF Core). These are write-side paths (login adds a token,
/// refresh rotates one), so the implementation tracks the entities it returns.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a refresh token by its value, with the owning User AND that user's other
    /// refresh tokens loaded — so the service can rotate this one and, on replay
    /// detection, revoke the whole family in one go.
    /// </summary>
    Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token, CancellationToken cancellationToken = default);
}
