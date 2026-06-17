using Application.DTOs.Auth;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Authentication endpoints. All are [AllowAnonymous] because they're how a caller
/// OBTAINS or refreshes a token — requiring one here would be a chicken-and-egg
/// problem. Like ProductsController, this stays thin: bind, call IAuthService, shape
/// the result; domain exceptions become status codes in the middleware.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Register a new user (role "User") and receive an initial token pair.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Log in with username + password and receive a token pair.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Exchange a valid refresh token for a brand-new token pair (rotates the old one).</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Revoke a refresh token so it can no longer be used (logout).</summary>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeAsync(request, cancellationToken);
        return NoContent();
    }
}
