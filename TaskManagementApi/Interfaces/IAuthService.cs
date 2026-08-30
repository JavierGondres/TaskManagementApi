using TaskManagementApi.DTOs.Auth;
using TaskManagementApi.DTOs.Users;

namespace TaskManagementApi.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto);

    Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto);

    Task<UserResponseDto?> GetCurrentUserAsync();
}
