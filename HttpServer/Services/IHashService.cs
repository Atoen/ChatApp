using HttpServer.Models;

namespace HttpServer.Services;

public interface IHashService
{
    Task<string> HashAsync(UserDto dto, byte[] salt);
}