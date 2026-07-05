using BookStoreApi.Data;
using BookStoreApi.Dtos.Books;
using BookStoreApi.Dtos.Common;
using BookStoreApi.Exceptions;
using BookStoreApi.Models;
using BookStoreApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApi.Services.Implementations;

public class BookService : IBookService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookService> _logger;

    public BookService(AppDbContext db, ILogger<BookService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<BookDto>> GetPagedAsync(BookQueryParameters query)
    {
        var books = _db.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            books = books.Where(b =>
                EF.Functions.Like(b.Title, $"%{term}%") ||
                (b.Description != null && EF.Functions.Like(b.Description, $"%{term}%")));
        }

        if (query.CategoryId.HasValue)
            books = books.Where(b => b.CategoryId == query.CategoryId.Value);

        if (query.AuthorId.HasValue)
            books = books.Where(b => b.AuthorId == query.AuthorId.Value);

        if (query.MinPrice.HasValue)
            books = books.Where(b => b.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            books = books.Where(b => b.Price <= query.MaxPrice.Value);

        books = ApplySort(books, query.SortBy);

        var totalCount = await books.CountAsync();

        var items = await books
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => ToDto(b))
            .ToListAsync();

        return new PagedResult<BookDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    private static IQueryable<Book> ApplySort(IQueryable<Book> books, string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return books.OrderBy(b => b.Title);

        var descending = sortBy.StartsWith('-');
        var field = descending ? sortBy[1..].ToLowerInvariant() : sortBy.ToLowerInvariant();

        return field switch
        {
            "price" => descending ? books.OrderByDescending(b => b.Price) : books.OrderBy(b => b.Price),
            "publisheddate" => descending ? books.OrderByDescending(b => b.PublishedDate) : books.OrderBy(b => b.PublishedDate),
            "title" => descending ? books.OrderByDescending(b => b.Title) : books.OrderBy(b => b.Title),
            _ => books.OrderBy(b => b.Title)
        };
    }

    public async Task<BookDto> GetByIdAsync(int id)
    {
        var book = await _db.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException(nameof(Book), id);

        return ToDto(book);
    }

    public async Task<BookDto> CreateAsync(CreateBookDto dto)
    {
        await EnsureAuthorAndCategoryExist(dto.AuthorId, dto.CategoryId);

        var book = new Book
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Isbn = dto.Isbn,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            PublishedDate = dto.PublishedDate,
            AuthorId = dto.AuthorId,
            CategoryId = dto.CategoryId
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        await _db.Entry(book).Reference(b => b.Author).LoadAsync();
        await _db.Entry(book).Reference(b => b.Category).LoadAsync();

        _logger.LogInformation("Book created: {BookId} - {Title}", book.Id, book.Title);
        return ToDto(book);
    }

    public async Task<BookDto> UpdateAsync(int id, UpdateBookDto dto)
    {
        var book = await _db.Books.FindAsync(id)
            ?? throw new NotFoundException(nameof(Book), id);

        await EnsureAuthorAndCategoryExist(dto.AuthorId, dto.CategoryId);

        book.Title = dto.Title.Trim();
        book.Description = dto.Description;
        book.Isbn = dto.Isbn;
        book.Price = dto.Price;
        book.StockQuantity = dto.StockQuantity;
        book.PublishedDate = dto.PublishedDate;
        book.AuthorId = dto.AuthorId;
        book.CategoryId = dto.CategoryId;

        await _db.SaveChangesAsync();
        await _db.Entry(book).Reference(b => b.Author).LoadAsync();
        await _db.Entry(book).Reference(b => b.Category).LoadAsync();

        _logger.LogInformation("Book updated: {BookId}", id);
        return ToDto(book);
    }

    public async Task DeleteAsync(int id)
    {
        var book = await _db.Books.FindAsync(id)
            ?? throw new NotFoundException(nameof(Book), id);

        var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.BookId == id);
        if (hasOrders)
            throw new BadRequestException("Cannot delete a book that appears in existing orders.");

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Book deleted: {BookId}", id);
    }

    private async Task EnsureAuthorAndCategoryExist(int authorId, int categoryId)
    {
        var authorExists = await _db.Authors.AnyAsync(a => a.Id == authorId);
        if (!authorExists)
            throw new ValidationAppException(nameof(authorId), $"Author with id '{authorId}' does not exist.");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
            throw new ValidationAppException(nameof(categoryId), $"Category with id '{categoryId}' does not exist.");
    }

    private static BookDto ToDto(Book b) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Description = b.Description,
        Isbn = b.Isbn,
        Price = b.Price,
        StockQuantity = b.StockQuantity,
        PublishedDate = b.PublishedDate,
        AuthorId = b.AuthorId,
        AuthorName = b.Author?.Name ?? string.Empty,
        CategoryId = b.CategoryId,
        CategoryName = b.Category?.Name ?? string.Empty
    };
}
