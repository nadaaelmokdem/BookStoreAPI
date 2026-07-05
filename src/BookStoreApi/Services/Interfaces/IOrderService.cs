using BookStoreApi.Dtos.Common;
using BookStoreApi.Dtos.Orders;

namespace BookStoreApi.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto);
    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int userId);
    Task<OrderDto> GetOrderByIdAsync(int orderId, int requestingUserId, bool isAdmin);
    Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page, int pageSize);
}
