using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Blazor.Server.Models;
using Blazor.Shared;
using Microsoft.IdentityModel.Tokens;

namespace Blazor.Server.Services;

public class TokenService
{
    public TimeSpan AuthTokenTimeSpan { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan RefreshTokenTimeSpan { get; set; } = TimeSpan.FromDays(7);
    
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly byte[] _key;
    private readonly AppDbContext _dbContext;
    
    public TokenService(IConfiguration configuration, AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _issuer = configuration["Jwt:Issuer"];
        _audience = configuration["Jwt:Audience"];
        _key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
    }

    public bool VerifyRefreshToken(User user, string token)
    {
        var hash = RefreshToken.HashData(token);
        
        var isValid = user.RefreshTokens.Any(x => x.Token == hash && x.IsActive);

        return isValid;
    }

    public RefreshToken CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var token = Convert.ToBase64String(bytes);

        var refreshToken = new RefreshToken
        {
            Token = token,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(RefreshTokenTimeSpan)
        };

        return refreshToken;
    }
    
    public string CreateAuthToken(params Claim[] claims)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(AuthTokenTimeSpan),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> RevokeRefreshToken(User user, string token)
    {
        var removed = user.RefreshTokens.RemoveAll(x => x.Token == token);
        if (removed == 0)
        {
            return false;
        }
        
        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task RevokeAllRefreshTokens(User user)
    {
        if (user.RefreshTokens.Count == 0) return;
        
        user.RefreshTokens.Clear();

        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();
    }
}