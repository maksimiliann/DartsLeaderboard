using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DartsLeaderboard.Web.Speech;

public sealed class SileroSpeechClient
{
    private readonly HttpClient _http;
    private readonly SpeechOptions _options;
    private readonly IMemoryCache _cache;

    public SileroSpeechClient(HttpClient http, IOptions<SpeechOptions> options, IMemoryCache cache)
    {
        _http = http;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Озвучивание выключено.");
        }

        var key = $"speech:{_options.Speaker}:{text}";
        if (_cache.TryGetValue(key, out byte[]? cached) && cached is { Length: > 0 })
        {
            return cached;
        }

        using var response = await _http.PostAsJsonAsync(
            "speak",
            new SpeakBody(text, _options.Speaker),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var wav = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        _cache.Set(key, wav, TimeSpan.FromHours(1));
        return wav;
    }

    private sealed record SpeakBody(string Text, string Speaker);
}
