namespace BookStoreApi.Exceptions;

/// <summary>Thrown when an authenticated user tries to access a resource they don't own. Mapped to HTTP 403.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
