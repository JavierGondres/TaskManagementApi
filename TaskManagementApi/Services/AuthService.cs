using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagementApi.Data;
using TaskManagementApi.DTOs.Auth;
using TaskManagementApi.DTOs.Users;
using TaskManagementApi.Interfaces;
using TaskManagementApi.Models;

namespace TaskManagementApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        ICurrentUser currentUser
    )
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var exists = await _db.Users.AnyAsync(user => user.Email == email);
        if (exists)
        {
            return null;
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ToAuthResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email == email);
        if (user is null)
        {
            return null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return ToAuthResponse(user);
    }

    public async Task<UserResponseDto?> GetCurrentUserAsync()
    {
        var userId = _currentUser.GetRequiredUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
        return user is null ? null : ToUserResponse(user);
    }

    private AuthResponseDto ToAuthResponse(User user)
    {
        return new AuthResponseDto { Token = _tokenService.CreateToken(user), User = ToUserResponse(user) };
    }

    private static UserResponseDto ToUserResponse(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
