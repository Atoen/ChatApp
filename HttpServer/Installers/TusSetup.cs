using System.Net;
using HttpServer.Hubs;
using HttpServer.Models;
using HttpServer.Services;
using Microsoft.AspNetCore.SignalR;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Stores;

namespace HttpServer.Installers;

public static class TusSetup
{
    public static void ConfigureTus(this IEndpointRouteBuilder app)
    {
        app.MapTus("/tus", httpContext => Task.FromResult(new DefaultTusConfiguration
        {
            Store = new TusDiskStore(@"D:\Tus\"),
            Events = new()
            {
                OnAuthorizeAsync = authorizeContext =>
                {
                    if (authorizeContext.HttpContext.User.Identity is not {IsAuthenticated: true})
                    {
                        authorizeContext.FailRequest(HttpStatusCode.Unauthorized);
                    }

                    return Task.CompletedTask;
                },
                
                OnFileCompleteAsync = async fileContext =>
                {
                    var file = await fileContext.GetFileAsync();
                    var embedService = httpContext.RequestServices.GetRequiredService<MessageEmbedService>();
                    var embed = await embedService.CreateEmbed(file, httpContext);

                    var hub = httpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
                    var message = new HubMessage
                    {
                        Author = httpContext.User.Identity!.Name!,
                        Timestamp = DateTimeOffset.Now,
                        Embed = embed
                    };

                    await hub.Clients.All.SendAsync("ReceiveMessage", message);
                }
            }
        }));
    }
}