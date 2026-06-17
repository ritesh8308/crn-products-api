namespace Domain.Exceptions;

/// <summary>
/// Thrown when authentication fails — bad credentials, or an invalid / expired /
/// already-used refresh token. API middleware maps this to HTTP 401. The message
/// is kept deliberately vague (e.g. "Invalid username or password") so we don't
/// reveal whether the username exists.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
