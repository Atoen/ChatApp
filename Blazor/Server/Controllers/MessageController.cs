using Blazor.Server.Models;
using Blazor.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessageController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async IAsyncEnumerable<MessageDto> GetMessages(int offset = 0)
    {
        var messages = CompiledQueries.MessagePageAsync(_context, offset);
        
        await foreach (var message in messages)
        {
            yield return message.ToDto();
        }
    }
}