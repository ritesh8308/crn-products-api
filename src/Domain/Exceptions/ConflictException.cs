namespace Domain.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with existing state — e.g. registering a
/// username that is already taken. API middleware maps this to HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
