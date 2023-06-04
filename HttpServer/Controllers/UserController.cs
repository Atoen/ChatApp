using Microsoft.AspNetCore.Mvc;
using HttpServer.Models;
using HttpServer.Services;

namespace HttpServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public async Task<ActionResult> LoginUser(UserDto userDto)
    {
        var result = await _userService.LoginAsync(userDto);
        var response = result.Match<ActionResult>(
            success => Ok(success.Value),
            notFound => NotFound(),
            unauthorized => Unauthorized());

        return response;
    }

    [HttpPost("signup")]
    public async Task<ActionResult> RegisterUser(UserDto userDto)
    {
        var result = await _userService.RegisterUser(userDto);
        var response = result.Match<ActionResult>(
            success => Ok(success.Value),
            conflict => Conflict(),
            validationError => BadRequest(validationError.Value),
            internalError => Problem(internalError.Value));

        return response;
    }
}