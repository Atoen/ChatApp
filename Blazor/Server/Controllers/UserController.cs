using System.Security.Claims;
using Blazor.Server.Models;
using Blazor.Server.Services;
using Blazor.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;
    private const string CookieName = "refreshToken";
    
    public UserController(UserService userService, TokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser(UserCredentialsDto userCredentialsDto)
    {
        var result = await _userService.LoginAsync(userCredentialsDto);
        var response = result.Match<IActionResult>(
            success => AuthSuccess(success.Value),
            notFound => NotFound(),
            unauthorized => Unauthorized());

        return response;
    }
    
    [HttpPost("signup")]
    public async Task<IActionResult> RegisterUser(UserCredentialsDto userCredentialsDto)
    {
        var result = await _userService.RegisterAsync(userCredentialsDto);
        var response = result.Match<IActionResult>(
            success => AuthSuccess(success.Value),
            conflict => Conflict(),
            validationError => BadRequest(validationError.Value),
            internalError => Problem(internalError.Value));

        return response;
    }
    
    private IActionResult AuthSuccess(ValueTuple<User, RefreshToken> tuple)
    {
        var (user, token) = tuple;
        AddUserRefreshTokenCookie(user, token);
        
        return Ok();
    }
    
    [HttpPost("token")]
    public async Task<IActionResult> LoginUsingToken()
    {
        var cookie = Request.Cookies[CookieName];
        var result = await _userService.VerifyRefreshTokenAsync(cookie);

        var response = result.Match<IActionResult>(
            success => Ok(CreateJwt(success.Value)),
            notFound => NotFound(),
            unauthorized => Unauthorized());
        
        return response;
    }

    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshJwt() => await LoginUsingToken();

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> RevokeToken([FromBody] string token, bool invalidateAllSessions = false)
    {
        var userResult = await _userService.GetUser(HttpContext.User);

        if (userResult.IsT1)
        {
            return NotFound();
        }
        
        var user = userResult.AsT0;
        
        if (invalidateAllSessions)
        {
            await _tokenService.RevokeAllRefreshTokens(user);
            return Ok();
        }
        
        var removed = await _tokenService.RevokeRefreshToken(user, token);
        Response.Cookies.Delete(CookieName);
        return removed ? Ok() : BadRequest();
    }
    
    private string CreateJwt(User user)
    {
        return _tokenService.CreateAuthToken(
            new Claim(JwtClaims.Username, user.Username),
            new Claim(JwtClaims.Role, "User"),
            new Claim(JwtClaims.Uid, user.Id.ToString()));
    }

    private void AddUserRefreshTokenCookie(User user, RefreshToken token)
    {
        var cookie = new CookieOptions
        {
            HttpOnly = true,
            Expires = token.Expires,
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        };

        var value = $"{user.Username} {token.Token}";
        Response.Cookies.Append(CookieName, value, cookie);
    }
}