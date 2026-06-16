namespace Domain.Exceptions;

/// <summary>
/// Thrown when domain/business validation fails.
/// API middleware maps this to HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
