﻿using Blazor.Shared;

namespace Blazor.Server.Services;

public interface IHashService
{
    Task<string> HashAsync(UserCredentialsDto credentialsDto, byte[] salt);

    byte[] GetSalt(int count);
}