using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using RestSharp;
using RestSharp.Authenticators;
using WpfClient.Extensions;
using WpfClient.Models;

namespace WpfClient.Services;

public class MessageService
{
    private const int PageSize = 20;

    private readonly RestClient _restClient;
    private readonly JwtAuthenticator _restAuthenticator;
    private readonly Func<string> _tokenProvider;
    private readonly Dispatcher _dispatcher;

    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly List<Message> _empty = new();
    private readonly TimeSpan _firstMessageTimeSpan = TimeSpan.FromMinutes(5);

    private int _currentPage;
    private int _offset;
    private bool _isNextPageAvailable = true;
    private Message? _lastMessage;

    public required Action<Message> MessageReceived { get; init; }
    public readonly Func<Message, Task> MessageReceivedInternal;

    public MessageService(Func<string> tokenProvider, Dispatcher dispatcher)
    {
        _tokenProvider = tokenProvider;
        _dispatcher = dispatcher;
        _restAuthenticator = new JwtAuthenticator(tokenProvider.Invoke());
        _restClient = new RestClient(new RestClientOptions
        {
            BaseUrl = new Uri(App.EndPointUri),
            Authenticator = _restAuthenticator
        });

        MessageReceivedInternal = MessageReceivedHandler;
    }

    private async Task MessageReceivedHandler(Message message)
    {
        _offset++;
        if (_offset >= PageSize)
        {
            _currentPage++;
            _offset = 0;
        }

        var formatted = await _dispatcher.Invoke(async () => await FormatMessageAsync(message));
        _lastMessage = formatted;
        _dispatcher.Invoke(() => MessageReceived.Invoke(formatted));
    }

    public async Task<List<Message>> GetNextMessagePageFormattedAsync()
    {
        if (!_isNextPageAvailable) return _empty;

        var page = await GetMessagePageAsync(_currentPage, _offset);
        if (_currentPage == 0 && _offset == 0) _lastMessage = page[^1];

        return await FormatMessagePageAsync(page);
    }

    private async Task<List<Message>> GetMessagePageAsync(int page, int offset = 0)
    {
        await _semaphore.WaitAsync();

        try
        {
            _restAuthenticator.SetBearerToken(_tokenProvider.Invoke());

            var request = new RestRequest($"api/Message?page={page}&offset={offset}");
            var response = await _restClient.GetAsync<List<Message>>(request);

            _isNextPageAvailable = response!.Count == PageSize;
            _currentPage++;

            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<List<Message>> FormatMessagePageAsync(List<Message> page)
    {
        var result = new List<Message>();

        await _dispatcher.Invoke(async () =>
        {
            foreach (var message in page)
            {
                await message.ParseEmbedDataAsync(_restClient);

                if (result.Count == 0)
                {
                    message.IsFirstMessage = true;
                    result.Add(message);
                    continue;
                }

                var lastMessage = result[^1];
                if (message.Author != lastMessage.Author ||
                    message.Timestamp.Subtract(lastMessage.Timestamp) > _firstMessageTimeSpan)
                {
                    message.IsFirstMessage = true;
                }

                result.Add(message);
            }
        });

        return result;
    }

    private async Task<Message> FormatMessageAsync(Message message)
    {
        await message.ParseEmbedDataAsync(_restClient);

        if (_lastMessage is null)
        {
            message.IsFirstMessage = true;
            return message;
        }

        if (message.Author != _lastMessage.Author ||
            message.Timestamp.Subtract(_lastMessage.Timestamp) > _firstMessageTimeSpan)
        {
            message.IsFirstMessage = true;
        }

        return message;
    }
}