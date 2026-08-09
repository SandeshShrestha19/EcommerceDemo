using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public class GeminiFacade : IGeminiFacade
{
  private const string FallbackModel = "gemini-2.5-flash-lite";
  private readonly HttpClient _httpClient;
  private readonly GeminiOptionsModel _options;
  private readonly IConfiguration _configuration;

  public GeminiFacade(HttpClient httpClient, IOptions<GeminiOptionsModel> options, IConfiguration configuration)
  {
    _httpClient = httpClient;
    _options = options.Value;
    _configuration = configuration;
  }

  public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
  {
    var apiKey = ResolveApiKey();
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      throw new InvalidOperationException(
        "Gemini API key is missing. Set the GEMINI_API_KEY environment variable (or Gemini:ApiKey in configuration).");
    }

    var model = ResolveModel();

    try
    {
      return await CallGenerateAsync(apiKey, model, prompt, cancellationToken);
    }
    catch (HttpRequestException) when (model != FallbackModel)
    {
      // A 404 means the configured model ID is stale/renamed. Retry with the
      // known-good model so a bad model value never breaks the AI feature.
      return await CallGenerateAsync(apiKey, FallbackModel, prompt, cancellationToken);
    }
  }

  private string ResolveApiKey()
  {
    // The key may arrive via appsettings ("Gemini:ApiKey"), a Render-style
    // "Gemini__ApiKey" env var, or a plain "GEMINI_API_KEY"-style variable.
    if (!string.IsNullOrWhiteSpace(_options.ApiKey)) return _options.ApiKey;

    return _configuration["GEMINI_API_KEY"]
        ?? _configuration["GEMINI_KEY"]
        ?? _configuration["GOOGLE_API_KEY"]
        ?? string.Empty;
  }

  private string ResolveModel()
  {
    return _configuration["Gemini:Model"]
        ?? FallbackModel;
  }

  private async Task<string> CallGenerateAsync(string apiKey, string model, string prompt, CancellationToken cancellationToken)
  {
    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    var requestBody = new
    {
      contents = new[]
      {
        new
          {
            parts = new[]
            {
              new { text = prompt }
            }
          }
        }
    };

    var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        throw new HttpRequestException($"Gemini model '{model}' was not found.", null, System.Net.HttpStatusCode.NotFound);
      }
      throw new Exception($"Gemini API error: {response.StatusCode}. Body: {responseText}");
    }

    using var json = JsonDocument.Parse(responseText);

    if (!json.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
    {
      throw new Exception("Gemini returned no content. The prompt may have been blocked.");
    }

    var text = candidates[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString();

    return text ?? string.Empty;
  }
}
