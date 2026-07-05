namespace BookStoreApi.Exceptions;

/// <summary>Thrown for authentication failures (e.g. bad credentials). Mapped to HTTP 401.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
