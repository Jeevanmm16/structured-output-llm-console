using System.Text.Json;
using StructuredOutputDemo.Models;

namespace StructuredOutputDemo.Services;

/// <summary>
/// Parses a raw LLM response string into a <see cref="ParseResult"/>.
///
/// Responsibility boundary:
///   - This class only handles JSON syntax and type correctness.
///   - Whether the resulting ticket has non-empty fields is the job of <see cref="TicketValidator"/>.
///
/// Key behaviour — missing fields:
///   JSON like <c>{"category":"Payment","priority":"High"}</c> deserializes successfully
///   into a <c>SupportTicket</c> with <c>Summary = ""</c> (System.Text.Json default).
///   This returns <see cref="ParseResult.Success"/>; the empty field is caught later by validation.
///   This distinction is intentional: valid JSON syntax ≠ valid structured output.
/// </summary>
public class JsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Attempts to parse <paramref name="rawJson"/> into a <see cref="SupportTicket"/>.
    /// </summary>
    /// <param name="rawJson">The raw string returned by the LLM.</param>
    /// <returns>A <see cref="ParseResult"/> describing success or the specific failure mode.</returns>
    public ParseResult TryParse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return new ParseResult.EmptyResponse();

        // Strip accidental markdown code fences the model may include despite instructions
        var cleaned = StripCodeFences(rawJson.Trim());

        try
        {
            var ticket = JsonSerializer.Deserialize<SupportTicket>(cleaned, Options);

            if (ticket is null)
                return new ParseResult.DeserializationFailed("Deserialization returned null.");

            return new ParseResult.Success(ticket);
        }
        catch (JsonException ex)
        {
            // Distinguish wrong-type from syntactically broken JSON by checking the message
            if (ex.Message.Contains("cannot be converted") ||
                ex.Message.Contains("is not valid") ||
                ex.Message.Contains("was not expected"))
            {
                return new ParseResult.WrongDataType(ex.Message);
            }

            return new ParseResult.MalformedJson(ex.Message);
        }
        catch (Exception ex)
        {
            return new ParseResult.DeserializationFailed(ex.Message);
        }
    }

    /// <summary>
    /// Removes markdown code fences (```json ... ``` or ``` ... ```) that the model
    /// sometimes adds despite being instructed not to.
    /// </summary>
    private static string StripCodeFences(string text)
    {
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
                text = text[(firstNewline + 1)..];

            if (text.EndsWith("```"))
                text = text[..^3].TrimEnd();
        }

        return text;
    }
}