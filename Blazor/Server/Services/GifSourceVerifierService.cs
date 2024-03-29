﻿namespace Blazor.Server.Services;

public class GifSourceVerifierService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GifSourceVerifierService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> VerifyAsync(string source)
    {
        if (!source.StartsWith("https://media.tenor.com/") &&
            !source.StartsWith("https://media.giphy.com/"))
        {
            return false;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return await VerifySourceAsync(uri);
    }

    private async Task<bool> VerifySourceAsync(Uri uri)
    {
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Head, uri);

        try
        {
            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}