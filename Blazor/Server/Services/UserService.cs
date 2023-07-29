using System.Security.Claims;
using Blazor.Server.Models;
using Blazor.Shared;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Blazor.Server.Services;

public class UserService
{
    private readonly AppDbContext _dbContext;
    private readonly IHashService _hashService;
    private readonly JwtTokenService _tokenService;
    private readonly IValidator<UserCredentialsDto> _validator;

    public UserService(AppDbContext dbContext, IHashService hashService, JwtTokenService tokenService, IValidator<UserCredentialsDto> validator)
    {
        _dbContext = dbContext;
        _hashService = hashService;
        _tokenService = tokenService;
        _validator = validator;
    }

    public async Task<OneOf<Success<string>, NotFound, Unauthorized>> LoginAsync(UserCredentialsDto userCredentialsDto)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == userCredentialsDto.Username);
        if (user is null)
        {
            return new NotFound();
        }

        var hash = await _hashService.HashAsync(userCredentialsDto, user.Salt);

        if (hash != user.PasswordHash)
        {
            return new Unauthorized();
        }

        var token = _tokenService.CreateTokenString(
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Sid, user.Id.ToString()));

        return new Success<string>(token);
    }

    public async Task<OneOf<Success<string>, Conflict, Error<List<ValidationFailure>>, Error<string>>> RegisterUser(UserCredentialsDto userCredentialsDto)
    {
        var validationResult = await _validator.ValidateAsync(userCredentialsDto);
        if (!validationResult.IsValid)
        {
            return new Error<List<ValidationFailure>>(validationResult.Errors);
        }

        if (await _dbContext.Users.AnyAsync(x => x.Username == userCredentialsDto.Username))
        {
            return new Conflict();
        }

        var createResult = await CreateUser(userCredentialsDto);

        return createResult.Match<OneOf<Success<string>, Conflict, Error<List<ValidationFailure>>, Error<string>>>(
            user =>
            {
                var token = _tokenService.CreateTokenString(
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, "user"),
                    new Claim(ClaimTypes.Sid, user.Id.ToString()));

                return new Success<string>(token);
            },
            conflict => conflict,
            error => error);
    }

    public async Task<OneOf<User, NotFound>> GetUser(ClaimsPrincipal claimsPrincipal)
    {
        var idClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
        if (idClaim is null) return new NotFound();

        var usernameClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
        if (usernameClaim is null) return new NotFound();
        
        var user = await _dbContext.Users.FindAsync(Guid.Parse(idClaim.Value));

        if (user is null || user.Username != usernameClaim.Value)
        {
            return new NotFound();
        }

        return user;
    }

    private async Task<OneOf<User, Conflict, Error<string>>> CreateUser(UserCredentialsDto userCredentialsDto)
    {
        var salt = _hashService.GetSalt(16);
        var hash = await _hashService.HashAsync(userCredentialsDto, salt);

        var user = new User
        {
            Username = userCredentialsDto.Username,
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