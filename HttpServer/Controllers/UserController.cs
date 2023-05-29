using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HttpServer.Models;
using HttpServer.Services;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace HttpServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IValidator<UserDto> _userValidator;
        private readonly ILogger<UserController> _logger;
        private readonly IHashService _hashService;
        private readonly JwtTokenService _tokenService;

        public UserController(AppDbContext dbContext,
            IValidator<UserDto> userValidator,
            ILogger<UserController> logger,
            IHashService hashService,
            JwtTokenService jwtTokenService)
        {
            _dbContext = dbContext;
            _userValidator = userValidator;
            _logger = logger;
            _hashService = hashService;
            _tokenService = jwtTokenService;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (_dbContext.Users == null)
            {
                return NotFound();
            }

            return await _dbContext.Users.ToListAsync();
        }
        
        // GET: api/User/5
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _dbContext.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }
        
        [HttpGet("isAvailable")]
        public async Task<ActionResult<bool>> CheckUsernameAvailability([FromQuery] string username)
        {
            var userExists = await _dbContext.Users.AnyAsync(x => x.Username == username);

            return !userExists;
        }

        [HttpPost("login")]
        public async Task<ActionResult> LoginUser(UserDto userDto)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == userDto.Username);
            
            if (user is null)
            {
                return Unauthorized();
            }

            var salt = user.Salt;
            var hash = await _hashService.HashAsync(userDto, salt);

            if (hash != user.PasswordHash)
            {
                return Unauthorized();
            }

            var token = _tokenService.CreateTokenString(
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "User"));

            return Ok(token);
        }
        
        [Authorize]
        [HttpGet("protected")]
        public IActionResult ProtectedEndpoint()
        {
            return Ok("Protected Resource");
        }
        
        [HttpPost("signup")]
        public async Task<ActionResult> RegisterUser(UserDto userDto)
        {
            var validationResult = await _userValidator.ValidateAsync(userDto);
            
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }
            
            if (await _dbContext.Users.AnyAsync(x => x.Username == userDto.Username))
            {
                return Conflict("User with specified username already exists.");
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = await _hashService.HashAsync(userDto, salt);

            var user = new User
            {
                Username = userDto.Username,
                Id = Guid.NewGuid(),
                PasswordHash = hash,
                Salt = salt
            };
            
            await _dbContext.Users.AddAsync(user);
            try
            {
                _logger.LogInformation("User '{User}' has registered", user.Username);
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserExists(user.Id))
                {
                    return Conflict();
                }
            
                throw;
            }
            
            return Ok();
        }

        private bool UserExists(Guid id)
        {
            return (_dbContext.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}