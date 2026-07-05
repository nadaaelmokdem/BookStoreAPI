namespace BookStoreApi.Dtos.Books;

/// <summary>
/// Query-string bound parameters for GET /api/books (browsing, searching, filtering, pagination).
/// </summary>
public class BookQueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? 10 : value;
    }

    /// <summary>Free-text search across title and description.</summary>
    public string? Search { get; set; }

    public int? CategoryId { get; set; }
    public int? AuthorId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>One of: title, price, publisheddate (prefix with "-" for descending, e.g. "-price").</summary>
    public string? SortBy { get; set; }
}
