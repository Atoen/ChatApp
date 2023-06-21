using HttpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace HttpServer.Services;

public class MessageService
{
    private readonly AppDbContext _dbContext;

    public MessageService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task StoreMessage(Message message)
    {
        await _dbContext.Messages.AddAsync(message);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {

        }
    }
}