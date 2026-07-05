namespace BookStoreApi.Dtos.Orders;

public class OrderItemRequestDto
{
    public int BookId { get; set; }
    public int Quantity { get; set; }
}
