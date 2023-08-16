using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Blazor.Shared;
using Blazored.LocalStorage;
using RestSharp;

namespace Blazor.Client.Services;

public sealed class JWTService
{
	private readonly RestClient _restClient;
	private readonly ILocalStorageService _localStorageService;
	private const string EmptyToken = "token";

	private JwtSecurityToken _content = null!;
	private readonly JwtSecurityTokenHandler _tokenHandler = new();
	private CancellationTokenSource _cancellationTokenSource = new();

	public Action? UnableToRefreshToken;
	private Task? _updateTask;

	public JWTService(RestClient restClient, ILocalStorageService localStorageService)
	{
		_restClient = restClient;
		_localStorageService = localStorageService;
	}

	public string Token { get; private set; } = EmptyToken;

	public void SetFirstToken(string token)
	{
		ArgumentException.ThrowIfNullOrEmpty(token);
		Token = token;

		_updateTask = TokenUpdateLoopAsync(true, _cancellationTokenSource.Token);
	}

	public bool IsTokenSet => Token != EmptyToken;

	public void ClearToken() => Token = EmptyToken;

	public async Task CancelPendingRequests()
	{
		_cancellationTokenSource.Cancel();

		if (_updateTask is not null)
		{
			await _updateTask;
		}

		_cancellationTokenSource = new CancellationTokenSource();
	}

	private string GetClaimValue(string claimName)
	{
		return _content.Claims.First(x => x.Type == claimName).Value;
	}

	private async Task TokenUpdateLoopAsync(bool startWithDelay, CancellationToken cancellationToken)
	{
		try
		{
			if (startWithDelay)
			{
				_content = _tokenHandler.ReadJwtToken(Token);
				await DelayNextRequest(cancellationToken);
			}

			while (!cancellationToken.IsCancellationRequested)
			{
				var token = await GetTokenAsync(cancellationToken);
				if (token is null) break;

				Token = token;
				_content = _tokenHandler.ReadJwtToken(token);
				await DelayNextRequest(cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			return;
		}

		UnableToRefreshToken?.Invoke();
	}

	private async Task DelayNextRequest(CancellationToken cancellationToken)
	{
		var lifetime = _content.ValidTo.ToLocalTime() - DateTime.Now;
		var requestDelay = lifetime * 0.8;

		await Task.Delay(requestDelay, cancellationToken);
	}

	private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
	{
		var delays = new[] { 1, 2, 5, 10, 15 };
		const int maxAttempts = 5;

		var attempt = 0;
		while (attempt < maxAttempts)
		{
			attempt++;

			var token = await RequestNewTokenAsync(cancellationToken);
			if (token is not null) return token;

			await Task.Delay(TimeSpan.FromSeconds(delays[attempt - 1]), cancellationToken);
		}

		return null;
	}

	private async Task<string?> RequestNewTokenAsync(CancellationToken cancellationToken)
	{
		try
		{
			var refreshToken = await _localStorageService.GetItemAsync<string>("Token", cancellationToken);

			var request = new RestRequest("api/user/refresh-token", Method.Post)
				.AddHeader("Authorization", $"Bearer {Token}")
				.AddBody(new RefreshTokenRequest
				{
					Username = GetClaimValue("unique_name"),
					Token = refreshToken
				});

			var response = await _restClient.ExecuteAsync(request, cancellationToken);

			return response.IsSuccessStatusCode
				? JsonSerializer.Deserialize<string>(response.Content!)
				: null;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
}