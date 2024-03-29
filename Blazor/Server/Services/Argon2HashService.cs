﻿using System.Security.Cryptography;
using System.Text;
using Blazor.Shared;
using Konscious.Security.Cryptography;

namespace Blazor.Server.Services;

public class Argon2HashService : IHashService
{
    public async Task<string> HashAsync(UserCredentialsDto credentialsDto, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(credentialsDto.PasswordHash!))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            MemorySize = 8192,
            Iterations = 8,
            AssociatedData = Encoding.UTF8.GetBytes(credentialsDto.Username!)
        };

        var hash = await argon2.GetBytesAsync(64);

        return Convert.ToBase64String(hash);
    }
    
    public byte[] GetSalt(int count) => RandomNumberGenerator.GetBytes(count);
}