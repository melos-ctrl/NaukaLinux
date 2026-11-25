using System.Text.Json.Serialization;

namespace Backend.Service;

public interface ITurnstileService
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default);
}

public sealed class TurnstileService : ITurnstileService
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private const int MaxTokenLength = 2048;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TurnstileService> _logger;

    public TurnstileService(
        HttpClient httpClient,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<TurnstileService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var secretKey = _configuration["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("Brak Turnstile:SecretKey - CAPTCHA nie jest sprawdzana (dozwolone tylko w trybie Development).");
                return true;
            }

            _logger.LogError("Brak Turnstile:SecretKey - odrzucam żądanie.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
            return false;

        var form = new Dictionary<string, string>
        {
            ["secret"] = secretKey,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
            form["remoteip"] = remoteIp;

        try
        {
            using var response = await _httpClient.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Cloudflare Turnstile zwrócił HTTP {StatusCode}.", (int)response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken);
            if (result?.Success == true)
                return true;

            _logger.LogInformation(
                "Turnstile odrzucił token: {ErrorCodes}",
                string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Nie udało się połączyć z Cloudflare Turnstile.");
            return false;
        }
    }

    private sealed record TurnstileVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}