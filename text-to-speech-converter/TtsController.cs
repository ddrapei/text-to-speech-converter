using Microsoft.AspNetCore.Mvc;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.TextToSpeech.v1;
using System.IO;


[ApiController]
[Route("api/[controller]")]
public class TtsController : ControllerBase
{
    private const string ApiKey = "";
    private const string ServiceUrl = "";

    [HttpPost("convert")]
    public IActionResult ConvertTextToSpeech([FromBody] TextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        // 1. Authenticate 
        var authenticator = new IamAuthenticator(ApiKey);
        var ttsService = new TextToSpeechService(authenticator);
        ttsService.SetServiceUrl(ServiceUrl);

        // 2. Call the API
        var result = ttsService.Synthesize(
            text: request.Text,
            accept: "audio/mp3",
            voice: "en-US_AllisonV3Voice" // change voices
        );

        // 3. Return the stream directly to the browser
        return File(result.Result, "audio/mp3", "speech.mp3");
    }
}