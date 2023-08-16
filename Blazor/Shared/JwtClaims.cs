using System.Security.Claims;

namespace Blazor.Shared;

public static class JwtClaims
{
    public const string Username = ClaimTypes.Name;
    public const string Role = ClaimTypes.Role;
    public const string Uid = "uid";
}