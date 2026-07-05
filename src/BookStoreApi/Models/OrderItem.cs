namespace BookStoreApi.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int Quantity { get; set; }

    // Price captured at time of purchase, so later price changes don't alter historical orders
    public decimal UnitPrice { get; set; }
}
