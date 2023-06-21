using Microsoft.AspNetCore.Mvc;
using HttpServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HttpServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<MessageController> _logger;

    public MessageController(AppDbContext context, ILogger<MessageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private const int PageSize = 20;

    [HttpGet]
    [Authorize]
    public async IAsyncEnumerable<MessageDto> GetMessages(int page, int offset = 0)
    {
        _logger.LogInformation("Getting message page for {User}", HttpContext.User.Identity?.Name);
        
        var messages = _context.Messages
            .OrderByDescending(m => m.Timestamp)
            .Skip(page * PageSize + offset)
            .Take(20)
            .Reverse()
            .Include(m => m.Author)
            .AsAsyncEnumerable();

        await foreach (var message in messages)
        {
            yield return message.ToDto();
        }
    }
}