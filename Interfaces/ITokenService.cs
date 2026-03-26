using TaskManager.API.Entities;

namespace TaskManager.API.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}