using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using HttpServer.Models;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace HttpServer.Services;

public class UserService
{
    private readonly AppDbContext _dbContext;
    private readonly IHashService _hashService;
    private readonly JwtTokenService _tokenService;
    private readonly IValidator<UserDto> _validator;

    public UserService(AppDbContext dbContext, IHashService hashService, JwtTokenService tokenService, IValidator<UserDto> validator)
    {
        _dbContext = dbContext;
        _hashService = hashService;
        _tokenService = tokenService;
        _validator = validator;
    }

    public async Task<OneOf<Success<string>, NotFound, Unauthorized>> LoginAsync(UserDto userDto)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == userDto.Username);
        if (user is null)
        {
            return new NotFound();
        }

        var hash = await _hashService.HashAsync(userDto, user.Salt);

        if (hash != user.PasswordHash)
        {
            return new Unauthorized();
        }

        var token = _tokenService.CreateTokenString(
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, "User"));

        return new Success<string>(token);
    }

    public async Task<OneOf<Success<string>, Conflict, Error<List<ValidationFailure>>, Error<string>>> RegisterUser(UserDto userDto)
    {
        var validationResult = await _validator.ValidateAsync(userDto);
        if (!validationResult.IsValid)
        {
            return new Error<List<ValidationFailure>>(validationResult.Errors);
        }

        if (await _dbContext.Users.AnyAsync(x => x.Username == userDto.Username))
        {
            return new Conflict();
        }

        var createResult = await CreateUser(userDto);
        return createResult.Match<OneOf<Success<string>, Conflict, Error<List<ValidationFailure>>, Error<string>>>(
            user =>
            {
                var token = _tokenService.CreateTokenString(
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, "User"));

                return new Success<string>(token);
            },
            conflict => conflict,
            error => error);
    }

    private async Task<OneOf<User, Conflict, Error<string>>> CreateUser(UserDto userDto)
    {
        var salt = _hashService.GetSalt(16);
        var hash = await _hashService.HashAsync(userDto, salt);

        var user = new User
        {
            Username = userDto.Username,
            PasswordHash = hash,
            Salt = salt,
            Id = Guid.NewGuid()
        };

        await _dbContext.Users.AddAsync(user);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            if (await _dbContext.Users.AnyAsync(x => x.Id == user.Id))
            {
                return new Conflict();
            }

            return new Error<string>(e.Message);
        }

        return user;
    }
}

public struct Unauthorized { }
public struct Conflict { }