using Microsoft.AspNetCore.Mvc;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.TextToSpeech.v1;
using System.IO;


[ApiController]
[Route("api/[controller]")]
public class TtsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public TtsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("convert")]
    public IActionResult ConvertTextToSpeech([FromBody] TextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        // Read API key and service URL from configuration
        var apiKey = _configuration["IBM:ApiKey"];
        var serviceUrl = _configuration["IBM:ServiceUrl"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(serviceUrl))
            return StatusCode(500, "IBM Watson credentials not configured.");

        // Get voice from request or use default
        var selectedVoice = string.IsNullOrWhiteSpace(request.Voice)
            ? "en-US_AllisonV3Voice"
            : request.Voice;

        Console.WriteLine($"Using voice: {selectedVoice}");

        // 1. Authenticate
        var authenticator = new IamAuthenticator(apiKey);
        var ttsService = new TextToSpeechService(authenticator);
        ttsService.SetServiceUrl(serviceUrl);

        // 2. Call the API
        var result = ttsService.Synthesize(
            text: request.Text,
            accept: "audio/mp3",
            voice: selectedVoice
        );

        // 3. Return the stream directly to the browser
        return File(result.Result, "audio/mp3", "speech.mp3");
    }
}