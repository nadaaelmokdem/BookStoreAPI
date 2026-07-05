using BookStoreApi.Data;
using BookStoreApi.Dtos.Authors;
using BookStoreApi.Exceptions;
using BookStoreApi.Models;
using Microsoft.EntityFrameworkCore;
using BookStoreApi.Services.Interfaces;

namespace BookStoreApi.Services.Implementations;

public class AuthorService : IAuthorService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuthorService> _logger;

    public AuthorService(AppDbContext db, ILogger<AuthorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AuthorDto>> GetAllAsync()
    {
        return await _db.Authors
            .OrderBy(a => a.Name)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    public async Task<AuthorDto> GetByIdAsync(int id)
    {
        var author = await _db.Authors.FindAsync(id)
            ?? throw new NotFoundException(nameof(Author), id);
        return ToDto(author);
    }

    public async Task<AuthorDto> CreateAsync(CreateAuthorDto dto)
    {
        var author = new Author { Name = dto.Name.Trim(), Bio = dto.Bio };
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Author created: {AuthorId} - {Name}", author.Id, author.Name);
        return ToDto(author);
    }

    public async Task<AuthorDto> UpdateAsync(int id, UpdateAuthorDto dto)
    {
        var author = await _db.Authors.FindAsync(id)
            ?? throw new NotFoundException(nameof(Author), id);

        author.Name = dto.Name.Trim();
        author.Bio = dto.Bio;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Author updated: {AuthorId}", id);
        return ToDto(author);
    }

    public async Task DeleteAsync(int id)
    {
        var author = await _db.Authors.FindAsync(id)
            ?? throw new NotFoundException(nameof(Author), id);

        var hasBooks = await _db.Books.AnyAsync(b => b.AuthorId == id);
        if (hasBooks)
            throw new BadRequestException("Cannot delete an author who still has books assigned.");

        _db.Authors.Remove(author);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Author deleted: {AuthorId}", id);
    }

    private static AuthorDto ToDto(Author a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Bio = a.Bio
    };
}
