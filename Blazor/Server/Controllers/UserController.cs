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
            success => Ok(success.Value),
            notFound => NotFound(),
            unauthorized => Unauthorized());

        return response;
    }
    
    [HttpPost("token")]
    public async Task<IActionResult> LoginUsingToken(RefreshTokenRequest request)
    {
        var result = await _userService.VerifyRefreshTokenAsync(request);

        var response = result.Match<IActionResult>(
            success => Ok(CreateJwt(success.Value)),
            notFound => NotFound(),
            unauthorized => Unauthorized());
        
        return response;
    }

    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshJwt(RefreshTokenRequest request) => await LoginUsingToken(request);

    [HttpPost("signup")]
    public async Task<IActionResult> RegisterUser(UserCredentialsDto userCredentialsDto)
    {
        var result = await _userService.RegisterUser(userCredentialsDto);
        var response = result.Match<IActionResult>(
            success => Ok(success.Value),
            conflict => Conflict(),
            validationError => BadRequest(validationError.Value),
            internalError => Problem(internalError.Value));

        return response;
    }
    
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
        return removed ? Ok() : BadRequest();
    }

    private string CreateJwt(User user)
    {
        return _tokenService.CreateAuthToken(
            new Claim(JwtClaims.Username, user.Username),
            new Claim(JwtClaims.Role, "User"),
            new Claim(JwtClaims.Uid, user.Id.ToString()));
    }
}