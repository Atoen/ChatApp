using FluentValidation;
using HealthChecks.UI.Client;
using HttpServer.Health;
using HttpServer.Hubs;
using HttpServer.Identity;
using Microsoft.EntityFrameworkCore;
using HttpServer.Models;
using HttpServer.Services;
using HttpServer.Setup;
using HttpServer.Validators;
using LiteX.HealthChecks.MariaDB;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using tusdotnet;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAuthentication();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(IdentityData.AdminUserPolicyName, policyBuilder =>
    {
        policyBuilder.RequireClaim(IdentityData.AdminUserClaimName, "true");
    });
});

var connectionString = builder.Configuration.GetConnectionString("MariaDB");

builder.Services.AddHealthChecks()
    .AddMariaDB(connectionString)
    .AddCheck<TusStoreHealthCheck>("TusStore");

builder.Services.AddControllers();
builder.Services.AddSignalR();

var serverVersion = new MySqlServerVersion(connectionString);
builder.Services.AddDbContext<AppDbContext>(optionsBuilder => optionsBuilder
    .UseMySql(connectionString, serverVersion)
    .EnableDetailedErrors()
);

builder.Services.AddValidatorsFromAssemblyContaining(typeof(MessageValidator));

builder.Services.AddTransient<IHashService, Argon2HashService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<TusDiskStoreHelper>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<EmbedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapHealthChecks("_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseAuthentication();
app.UseAuthorization();

app.MapTus("/tus", Tus.TusConfigurationFactory);

app.MapControllers();
app.MapHub<ChatHub>("/chat");

app.Run();