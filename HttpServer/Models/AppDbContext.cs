using Microsoft.EntityFrameworkCore;

namespace HttpServer.Models;

public class AppDbContext : DbContext
{
    public DbContextOptions<AppDbContext> Options { get; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Options = options;
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Message> Messages { get; set; } = default!;
}