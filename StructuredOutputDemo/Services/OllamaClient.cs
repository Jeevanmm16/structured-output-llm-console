using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StructuredOutputDemo.Services.Interfaces;

namespace StructuredOutputDemo.Services;

/// <summary>
/// Sends prompts to a locally running Ollama instance via its REST API.
/// Endpoint: POST http://localhost:11434/api/generate
///
/// To change the model, update <see cref="Model"/>. Run <c>ollama list</c> to see
/// what is installed. Recommended models for structured extraction: llama3.2:latest, mistral:7b.
/// </summary>
public class OllamaClient : ILlmClient
{
    private const string Model = "llama3.2:latest";
    private const string BaseUrl = "http://localhost:11434/api/generate";

    private readonly HttpClient _httpClient;

    public OllamaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(string prompt)
    {
        var requestBody = new OllamaRequest(Model, prompt, Stream: false);

        HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(BaseUrl, requestBody);
        httpResponse.EnsureSuccessStatusCode();

        string responseJson = await httpResponse.Content.ReadAsStringAsync();
        OllamaResponse? ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

        if (ollamaResponse is null || string.IsNullOrWhiteSpace(ollamaResponse.Response))
            return string.Empty;

        return ollamaResponse.Response.Trim();
    }

    // -------------------------------------------------------------------------
    // Private DTOs for Ollama's API — not part of the domain model
    // -------------------------------------------------------------------------

    private record OllamaRequest(
        [property: JsonPropertyName("model")]  string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
    }
}
