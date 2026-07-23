namespace StructuredOutputDemo.Services.Interfaces;

/// <summary>
/// Abstraction over any LLM backend.
/// The production implementation uses Ollama; tests inject <c>FakeLlmClient</c>.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Sends <paramref name="prompt"/> to the LLM and returns the raw text response.
    /// </summary>
    /// <param name="prompt">The complete prompt to send.</param>
    /// <returns>The raw string response from the model.</returns>
    Task<string> GenerateAsync(string prompt);
}