using BookStoreApi.Dtos.Authors;

namespace BookStoreApi.Services.Interfaces;

public interface IAuthorService
{
    Task<IReadOnlyList<AuthorDto>> GetAllAsync();
    Task<AuthorDto> GetByIdAsync(int id);
    Task<AuthorDto> CreateAsync(CreateAuthorDto dto);
    Task<AuthorDto> UpdateAsync(int id, UpdateAuthorDto dto);
    Task DeleteAsync(int id);
}
