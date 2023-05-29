using System.Text;
using HttpServer.Models;
using Konscious.Security.Cryptography;

namespace HttpServer.Services;

public class Argon2HashService : IHashService
{
    public async Task<string> HashAsync(UserDto dto, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(dto.PasswordHash!));
        argon2.Salt = salt;
        argon2.DegreeOfParallelism = 8;
        argon2.MemorySize = 8192;
        argon2.Iterations = 8;
        argon2.AssociatedData = Encoding.UTF8.GetBytes(dto.Username!);
            
        var hash = await argon2.GetBytesAsync(64);
        return Convert.ToBase64String(hash);
    }
}