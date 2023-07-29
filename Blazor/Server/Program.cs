//using Microsoft.AspNetCore.ResponseCompression;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseWebAssemblyDebugging();
//}
//else
//{
//    app.UseExceptionHandler("/Error");
//}

//app.UseBlazorFrameworkFiles();
//app.UseStaticFiles();

//app.UseRouting();


//app.MapRazorPages();
//app.MapControllers();
//app.MapFallbackToFile("index.html");

//app.Run();

using Blazor.Server.Health;
using Blazor.Server.Hubs;
using Blazor.Server.Identity;
using Blazor.Server.Models;
using Blazor.Server.Services;
using Blazor.Server.Setup;
using Blazor.Server.Validators;
using Microsoft.AspNetCore.ResponseCompression;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore;
using LiteX.HealthChecks.MariaDB;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using tusdotnet;

var builder = WebApplication.CreateBuilder(args);

var corsPolicy = "CorsPolicy";

builder.ConfigureAuthentication();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(IdentityData.AdminUserPolicyName, policyBuilder =>
    {
        policyBuilder.RequireClaim(IdentityData.AdminUserClaimName, "true");
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy, policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseCors(corsPolicy);

app.MapHealthChecks("_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapTus("/tus", Tus.TusConfigurationFactory);

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chat");

app.Run();
