using FluentValidation;
using HttpServer.Hubs;
using HttpServer.Installers;
using Microsoft.EntityFrameworkCore;
using HttpServer.Models;
using HttpServer.Services;
using HttpServer.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.AddAuthentication();

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(x => x.UseNpgsql(builder.CreatePostgresConnectionString()));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(MessageValidator));
builder.Services.AddTransient<IHashService, Argon2HashService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/Chat");

app.Run();