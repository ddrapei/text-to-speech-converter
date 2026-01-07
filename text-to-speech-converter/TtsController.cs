using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.TextToSpeech.v1;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class TtsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    // Inject configuration and memory cache dependencies
    public TtsController(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
    }

    // Handles text-to-speech conversion with caching strategy to minimize external API calls
    [HttpPost("convert")]
    public async Task<IActionResult> ConvertTextToSpeech([FromBody] TextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        // Generate a deterministic cache key based on voice and text hash
        string cacheKey = $"tts_{request.Voice}_{request.Text.GetHashCode()}";

        // Check if the audio is already cached to avoid re-processing
        if (_cache.TryGetValue(cacheKey, out byte[] cachedAudio))
        {
            Console.WriteLine("Cache Hit: Returning stored audio.");
            return File(cachedAudio, "audio/mp3", "speech.mp3");
        }

        // --- Cache Miss: Proceed to call external IBM Watson Service ---

        var apiKey = _configuration["IBM:ApiKey"];
        var serviceUrl = _configuration["IBM:ServiceUrl"];

        // Validate API credentials before attempting connection
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(serviceUrl))
            return StatusCode(500, "IBM Watson credentials not configured.");

        var selectedVoice = request.Voice ?? "en-US_AllisonV3Voice";

        // Initialize IBM Watson authentication and service client
        var authenticator = new IamAuthenticator(apiKey);
        var ttsService = new TextToSpeechService(authenticator);
        ttsService.SetServiceUrl(serviceUrl);

        // Execute external API call
        var result = ttsService.Synthesize(
            text: request.Text,
            accept: "audio/mp3",
            voice: selectedVoice
        );

        // Convert stream to byte array to enable caching and reuse
        using (var memoryStream = new MemoryStream())
        {
            result.Result.CopyTo(memoryStream);
            byte[] audioBytes = memoryStream.ToArray();

            // Store result in cache with an absolute expiration of 1 hour
            _cache.Set(cacheKey, audioBytes, TimeSpan.FromHours(1));

            Console.WriteLine("Cache Miss: Fetched from IBM Watson and stored in cache.");
            return File(audioBytes, "audio/mp3", "speech.mp3");
        }
    }
}