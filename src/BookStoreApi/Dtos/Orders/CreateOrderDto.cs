namespace BookStoreApi.Dtos.Orders;

public class CreateOrderDto
{
    public List<OrderItemRequestDto> Items { get; set; } = new();
}
