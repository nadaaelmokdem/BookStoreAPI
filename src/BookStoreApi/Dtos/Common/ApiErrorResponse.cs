namespace BookStoreApi.Dtos.Common;

/// <summary>
/// Structured error response returned for every non-2xx result.
/// </summary>
public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }

    /// <summary>Field-level validation errors, if any (field name -> list of messages).</summary>
    public IDictionary<string, string[]>? Errors { get; set; }
}
