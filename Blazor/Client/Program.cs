using Blazor.Client;
using Blazor.Client.Options;
using Blazor.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RestSharp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddScoped(_ => new RestClient(options =>
{
    options.BaseUrl = new Uri("http://squadtalk.ddns.net");
}));
builder.Services.AddTransient<SignalRService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<JWTService>();
builder.Services.Configure<JwtServiceOptions>(options =>
{
    options.RetryAttempts = 5;
    options.RetryDelays = new[] { 1, 2, 5, 10, 15 };
});

builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
