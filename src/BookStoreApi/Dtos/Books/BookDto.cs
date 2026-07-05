namespace BookStoreApi.Dtos.Books;

public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Isbn { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime? PublishedDate { get; set; }

    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
