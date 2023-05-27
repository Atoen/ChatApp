using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HttpServer.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HttpServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<string> _usernameValidator;

        public UserController(AppDbContext context, IValidator<string> usernameValidator)
        {
            _context = context;
            _usernameValidator = usernameValidator;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            return await _context.Users.ToListAsync();
        }

        // GET: api/User/5
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(string username)
        {
            var validationResult = await _usernameValidator.ValidateAsync(username);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            if (_context.Users.Any(x => x.Username == username))
            {
                return Conflict();
            }

            var user = new User
            {
                Username = username,
                Id = Guid.NewGuid()
            };

            await _context.Users.AddAsync(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserExists(user.Id))
                {
                    return Conflict();
                }

                throw;
            }
            
            return Ok(user);
        }

        private bool UserExists(Guid id)
        {
            return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}