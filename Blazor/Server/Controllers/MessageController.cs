using Blazor.Server.Models;
using Blazor.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blazor.Server.Controllers;

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

    [HttpGet]
    [Authorize]
    public async IAsyncEnumerable<MessageDto> GetMessages(int offset = 0)
    {
        var messages = _context.Messages
            .OrderByDescending(m => m.Timestamp)
            .Skip(offset)
            .Take(20)
            .Include(m => m.Author)
            .AsAsyncEnumerable();

        await foreach (var message in messages)
        {
            yield return message.ToDto();
        }
    }
}