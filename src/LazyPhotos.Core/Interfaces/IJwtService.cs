using LazyPhotos.Core.Entities;

namespace LazyPhotos.Core.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    int? ValidateToken(string token);
}
