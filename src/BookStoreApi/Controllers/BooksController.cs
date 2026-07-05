using BookStoreApi.Dtos.Books;
using BookStoreApi.Dtos.Common;
using BookStoreApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStoreApi.Controllers;

/// <summary>Browse, search, and (for admins) manage books.</summary>
[ApiController]
[Route("api/books")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    /// <summary>Browse books with optional search, filtering, sorting, and pagination.</summary>
    /// <remarks>
    /// Query parameters: page, pageSize, search, categoryId, authorId, minPrice, maxPrice, sortBy
    /// (sortBy accepts "title", "price", "publishedDate"; prefix with "-" for descending, e.g. "-price").
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<BookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookDto>>> GetBooks([FromQuery] BookQueryParameters query)
    {
        var result = await _bookService.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>Get a single book's details.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetById(int id)
    {
        var result = await _bookService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Create a new book. Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BookDto>> Create([FromBody] CreateBookDto dto)
    {
        var result = await _bookService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update an existing book. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> Update(int id, [FromBody] UpdateBookDto dto)
    {
        var result = await _bookService.UpdateAsync(id, dto);
        return Ok(result);
    }

    /// <summary>Delete a book. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        await _bookService.DeleteAsync(id);
        return NoContent();
    }
}
