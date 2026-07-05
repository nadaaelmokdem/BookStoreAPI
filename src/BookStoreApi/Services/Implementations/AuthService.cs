using BookStoreApi.Data;
using BookStoreApi.Dtos.Auth;
using BookStoreApi.Enums;
using BookStoreApi.Exceptions;
using BookStoreApi.Models;
using BookStoreApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApi.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (exists)
        {
            _logger.LogWarning("Registration attempt with already-used email {Email}", normalizedEmail);
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Customer
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email} (Id={UserId})", user.Email, user.Id);

        var (token, expires) = _tokenService.GenerateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expires,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", normalizedEmail);
            throw new UnauthorizedException("Invalid email or password.");
        }

        _logger.LogInformation("User logged in: {Email} (Id={UserId})", user.Email, user.Id);

        var (token, expires) = _tokenService.GenerateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expires,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }
}
