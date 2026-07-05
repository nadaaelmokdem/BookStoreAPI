namespace BookStoreApi.Exceptions;

/// <summary>Thrown for business-rule violations that are the client's fault. Mapped to HTTP 400.</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
