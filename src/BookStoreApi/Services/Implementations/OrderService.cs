using BookStoreApi.Data;
using BookStoreApi.Dtos.Common;
using BookStoreApi.Dtos.Orders;
using BookStoreApi.Enums;
using BookStoreApi.Exceptions;
using BookStoreApi.Models;
using BookStoreApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApi.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            throw new ValidationAppException(nameof(dto.Items), "An order must contain at least one item.");

        var bookIds = dto.Items.Select(i => i.BookId).Distinct().ToList();
        var books = await _db.Books.Where(b => bookIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id);

        var missing = bookIds.Where(id => !books.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new ValidationAppException("items", $"Book(s) not found: {string.Join(", ", missing)}");

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            OrderDateUtc = DateTime.UtcNow
        };

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
                throw new ValidationAppException("items", $"Quantity for book {item.BookId} must be greater than zero.");

            var book = books[item.BookId];
            if (book.StockQuantity < item.Quantity)
                throw new BadRequestException($"Insufficient stock for '{book.Title}'. Available: {book.StockQuantity}, requested: {item.Quantity}.");

            book.StockQuantity -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                BookId = book.Id,
                Quantity = item.Quantity,
                UnitPrice = book.Price
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} created by user {UserId} for {Total:C} ({ItemCount} item(s))",
            order.Id, userId, order.TotalAmount, order.Items.Count);

        return await GetOrderByIdAsync(order.Id, userId, isAdmin: true);
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int userId)
    {
        var orders = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Book)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDateUtc)
            .ToListAsync();

        return orders.Select(o => ToDto(o, includeCustomerEmail: false)).ToList();
    }

    public async Task<OrderDto> GetOrderByIdAsync(int orderId, int requestingUserId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Book)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!isAdmin && order.UserId != requestingUserId)
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to another customer", requestingUserId, orderId);
            throw new ForbiddenException("You do not have access to this order.");
        }

        return ToDto(order, includeCustomerEmail: isAdmin);
    }

    public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page, int pageSize)
    {
        var query = _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Book)
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDateUtc);

        var totalCount = await query.CountAsync();

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = orders.Select(o => ToDto(o, includeCustomerEmail: true)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static OrderDto ToDto(Order o, bool includeCustomerEmail) => new()
    {
        Id = o.Id,
        OrderDateUtc = o.OrderDateUtc,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        CustomerEmail = includeCustomerEmail ? o.User?.Email : null,
        Items = o.Items.Select(i => new OrderItemDto
        {
            BookId = i.BookId,
            BookTitle = i.Book?.Title ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
