using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Blazor.Client;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly JWTService _jwtService;
    private AuthenticationState? _authenticationState;

    public JwtAuthenticationStateProvider(JWTService jwtService)
    {
        _jwtService = jwtService;
        _jwtService.TokenUpdated += JwtServiceOnTokenUpdated;
    }

    private void JwtServiceOnTokenUpdated(JwtSecurityToken securityToken)
    {
        var identity = new ClaimsIdentity(securityToken.Claims);
        var principal = new ClaimsPrincipal(identity);

        _authenticationState = new AuthenticationState(principal);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_authenticationState is null)
        {
            var identity = new ClaimsIdentity(_jwtService.SecurityToken.Claims);
            var principal = new ClaimsPrincipal(identity);

            _authenticationState = new AuthenticationState(principal);
        }
        
        return Task.FromResult(_authenticationState);
    }

    public void Dispose()
    {
        _jwtService.TokenUpdated -= JwtServiceOnTokenUpdated;
    }
}