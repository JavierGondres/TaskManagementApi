using TaskManagementApi.DTOs.Users;

namespace TaskManagementApi.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public UserResponseDto User { get; set; } = null!;
}
