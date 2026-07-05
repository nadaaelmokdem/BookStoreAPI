using BookStoreApi.Models;

namespace BookStoreApi.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
