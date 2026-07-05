using BookStoreApi.Dtos.Common;
using BookStoreApi.Dtos.Orders;
using BookStoreApi.Extensions;
using BookStoreApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStoreApi.Controllers;

/// <summary>Place orders and view order history.</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Place a new order containing one or more books. Requires authentication.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
    {
        var userId = User.GetUserId();
        var result = await _orderService.CreateOrderAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get the current customer's own past orders.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders()
    {
        var userId = User.GetUserId();
        return Ok(await _orderService.GetMyOrdersAsync(userId));
    }

    /// <summary>Get a specific order. Customers may only view their own; admins may view any.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        return Ok(await _orderService.GetOrderByIdAsync(id, userId, isAdmin));
    }

    /// <summary>Get every order placed by every customer. Admin only.</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _orderService.GetAllOrdersAsync(page, pageSize));
    }
}
