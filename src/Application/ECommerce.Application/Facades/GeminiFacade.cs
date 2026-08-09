using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public class GeminiFacade : IGeminiFacade
{
  // Models to fall back to, in order, when the configured model is not found.
  // The 2.5 series is being superseded by newer generation models, so the list
  // covers both so a renamed/unavailable model never breaks the AI feature.
  private static readonly string[] FallbackModels =
  {
    "gemini-2.5-flash-lite",
    "gemini-3.1-flash-lite",
    "gemini-3.5-flash-lite",
    "gemini-2.5-flash",
    "gemini-2.0-flash-lite",
    "gemini-2.0-flash",
    "gemini-1.5-flash"
  };

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

    // Try the configured model first, then each fallback until one responds.
    // Only a 404 (model not found) moves on to the next candidate; key/quota
    // errors (400/403/429) propagate immediately because retrying another
    // model would not help.
    var models = new List<string>();
    var configured = _configuration["Gemini:Model"];
    if (!string.IsNullOrWhiteSpace(configured)) models.Add(configured);
    foreach (var candidate in FallbackModels)
    {
      if (!models.Contains(candidate)) models.Add(candidate);
    }

    List<string> notFound = new();
    foreach (var model in models)
    {
      try
      {
        return await CallGenerateAsync(apiKey, model, prompt, cancellationToken);
      }
      catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
      {
        notFound.Add(model);
      }
    }

    throw new Exception(
      $"Gemini returned 'model not found' for: {string.Join(", ", notFound)}. " +
      "The API key is valid but none of those models are available to it; check the Gemini model config on the server.");
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
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new HttpRequestException($"Gemini model '{model}' was not found.", null, HttpStatusCode.NotFound);
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
