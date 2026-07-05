namespace BookStoreApi.Exceptions;

/// <summary>Thrown for manual/service-level validation failures. Mapped to HTTP 400 with field errors.</summary>
public class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationAppException(string field, string error)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]> { [field] = new[] { error } };
    }
}
