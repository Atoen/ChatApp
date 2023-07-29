namespace Blazor.Client.Services;

public sealed class JWTService
{
    public string Token { get; private set; } = "Token";

    public void SetToken(string token) => Token = token;
}