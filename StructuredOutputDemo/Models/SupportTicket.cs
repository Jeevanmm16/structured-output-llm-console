using System.Text.Json.Serialization;

namespace StructuredOutputDemo.Models;

/// <summary>
/// Represents the structured data extracted from a support ticket by the LLM.
/// All three fields are required; the validator will reject any that are empty.
/// </summary>
public class SupportTicket
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}