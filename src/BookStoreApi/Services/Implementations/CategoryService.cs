using BookStoreApi.Data;
using BookStoreApi.Dtos.Categories;
using BookStoreApi.Exceptions;
using BookStoreApi.Models;
using Microsoft.EntityFrameworkCore;
using BookStoreApi.Services.Interfaces;

namespace BookStoreApi.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(AppDbContext db, ILogger<CategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        return await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id)
            ?? throw new NotFoundException(nameof(Category), id);
        return ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var nameExists = await _db.Categories.AnyAsync(c => c.Name == dto.Name.Trim());
        if (nameExists)
            throw new ConflictException($"A category named '{dto.Name}' already exists.");

        var category = new Category { Name = dto.Name.Trim(), Description = dto.Description };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Category created: {CategoryId} - {Name}", category.Id, category.Name);
        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _db.Categories.FindAsync(id)
            ?? throw new NotFoundException(nameof(Category), id);

        category.Name = dto.Name.Trim();
        category.Description = dto.Description;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Category updated: {CategoryId}", id);
        return ToDto(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id)
            ?? throw new NotFoundException(nameof(Category), id);

        var hasBooks = await _db.Books.AnyAsync(b => b.CategoryId == id);
        if (hasBooks)
            throw new BadRequestException("Cannot delete a category that still has books assigned.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Category deleted: {CategoryId}", id);
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description
    };
}
