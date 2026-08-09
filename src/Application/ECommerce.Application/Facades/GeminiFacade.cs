using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public class GeminiFacade : IGeminiFacade
{
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
    // The key may arrive via appsettings ("Gemini:ApiKey"), a Render-style
    // "Gemini__ApiKey" env var, or a plain "GEMINI_API_KEY" env var. Try all
    // three so a single well-known variable name works on any host.
    var apiKey = _options.ApiKey;
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      apiKey = _configuration["GEMINI_API_KEY"] ?? string.Empty;
    }
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      throw new InvalidOperationException("Gemini API key is missing.");
    }
    var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash-lite-001";

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
      throw new Exception($"Gemini API error: {response.StatusCode}. Body: {responseText}");
    }

    using var json = JsonDocument.Parse(responseText);

    var text = json.RootElement
        .GetProperty("candidates")[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString();

    return text ?? string.Empty;
  }
}