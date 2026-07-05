namespace BookStoreApi.Dtos.Orders;

public class OrderDto
{
    public int Id { get; set; }
    public DateTime OrderDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    /// <summary>Populated for admins viewing all orders; the owner's own listing can ignore it.</summary>
    public string? CustomerEmail { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}
