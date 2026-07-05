using BookStoreApi.Dtos.Books;
using BookStoreApi.Dtos.Common;

namespace BookStoreApi.Services.Interfaces;

public interface IBookService
{
    Task<PagedResult<BookDto>> GetPagedAsync(BookQueryParameters query);
    Task<BookDto> GetByIdAsync(int id);
    Task<BookDto> CreateAsync(CreateBookDto dto);
    Task<BookDto> UpdateAsync(int id, UpdateBookDto dto);
    Task DeleteAsync(int id);
}
