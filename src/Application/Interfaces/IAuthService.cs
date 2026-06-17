using Application.DTOs.Auth;

namespace Application.Interfaces;

/// <summary>
/// Authentication use cases the API's AuthController calls. Speaks only in DTOs.
/// Implemented in the Application layer (AuthService); depends only on the other
/// interfaces here, so it stays unit-testable with mocks.
/// </summary>
public interface IAuthService
{
    /// <summary>Creates a new user (role "User") and immediately issues a token pair.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies credentials and issues a token pair.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Validates a refresh token and rotates it, returning a brand-new token pair.</summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token so it can no longer be exchanged (logout).</summary>
    Task RevokeAsync(RevokeRequest request, CancellationToken cancellationToken = default);
}
