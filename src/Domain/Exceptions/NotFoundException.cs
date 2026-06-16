namespace Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist.
/// API middleware (Phase 5) maps this to HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"{name} with id {key} was not found.") { }
}
