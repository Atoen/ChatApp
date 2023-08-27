using System.Security.Claims;
using Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Blazor.Client;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly JWTService _jwtService;

    public JwtAuthenticationStateProvider(JWTService jwtService)
    {
        _jwtService = jwtService;
    }
    
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity(_jwtService.SecurityToken.Claims);
        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }
}