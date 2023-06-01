using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HttpServer.Models;

namespace HttpServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<Message> _messageValidator;

        public MessageController(AppDbContext context, IValidator<Message> messageValidator)
        {
            _context = context;
            _messageValidator = messageValidator;
        }
        // GET: api/Message?id=5
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessagesByUser([FromQuery] string authorId)
        {
            var messages = await _context.Messages
                .Where(x => x.Author.Id == authorId)
                .ToListAsync();

            if (messages.Count == 0)
            {
                return NotFound();
            }

            return messages;
        }

        // POST: api/Message
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Message>> PostMessage(Message message)
        {
            var validationResult = await _messageValidator.ValidateAsync(message);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            
            return Ok();
        }
    }
}