using HttpServer.Models;

namespace HttpServer.Services;

public interface IHashService
{
    Task<string> HashAsync(UserCredentialsDto credentialsDto, byte[] salt);

    byte[] GetSalt(int count);
}