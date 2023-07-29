using Blazor.Client.Extensions;
using Blazor.Client.Models;
using Blazor.Shared;
using RestSharp;
using RestSharp.Authenticators;

namespace Blazor.Client.Services;

public sealed class MessageService
{
    private readonly JWTService _jwtService;
    private readonly RestClient _restClient;
    private readonly JwtAuthenticator _restAuthenticator;
    private readonly TimeSpan _firstMessageTimeSpan = TimeSpan.FromMinutes(5);

    private int _offset;
    private MessageModel? _lastMessage;

    public Action<MessageModel>? MessageReceived { get; set; }

    public MessageService(JWTService jwtService)
    {
        _jwtService = jwtService;
        _restAuthenticator = new JwtAuthenticator(_jwtService.Token);
        _restClient = new RestClient(new RestClientOptions
        {
            BaseUrl = new Uri("http://squadtalk.ddns.net"),
            Authenticator = _restAuthenticator
        });
    }

    public async Task HandleIncomingMessage(MessageDto messageDto)
    {
        var formatted = await FormatMessageAsync(messageDto);
        _lastMessage = formatted;

        MessageReceived?.Invoke(formatted);
    }

    public async Task<IList<MessageModel>> GetMessagePageAsync(int requestOffset)
    {
        var pageOffset = _offset + requestOffset;

        _restAuthenticator.SetBearerToken(_jwtService.Token);
        var restRequest = new RestRequest($"api/Message?offset={pageOffset}");
        var response = await _restClient.GetAsync<List<MessageDto>>(restRequest);
        
        var page = new List<MessageModel>(response!.Count);
        foreach (var dto in response)
        {
            var model = await FormatMessageAsync(dto);
            page.Add(model);
            _lastMessage = model;
        }

        if (pageOffset == 0) _lastMessage = page[^1];

        return page;
    }

    public void CheckIfIsFirst(MessageModel previous, MessageModel next)
    {
        previous.IsFirst = previous.Author != next.Author ||
                           previous.Timestamp.Subtract(next.Timestamp) > _firstMessageTimeSpan;
    }

    private async Task<MessageModel> FormatMessageAsync(MessageDto messageDto)
    {
        var model = await messageDto.ToModelAsync(_restClient);

        if (_lastMessage is null)
        {
            model.IsFirst = true;
            return model;
        }

        if (model.Author != _lastMessage.Author ||
            model.Timestamp.Subtract(_lastMessage.Timestamp) > _firstMessageTimeSpan)
        {
            model.IsFirst = true;
        }

        return model;
    }
}