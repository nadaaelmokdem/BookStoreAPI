namespace BookStoreApi.Dtos.Books;

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Isbn { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime? PublishedDate { get; set; }
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
}
