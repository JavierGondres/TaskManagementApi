using TaskManagementApi.Models;

namespace TaskManagementApi.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
