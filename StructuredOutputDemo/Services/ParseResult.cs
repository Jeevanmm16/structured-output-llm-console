using StructuredOutputDemo.Models;

namespace StructuredOutputDemo.Services;

/// <summary>
/// Discriminated union returned by <see cref="JsonParser.TryParse"/>.
/// Each case maps to a distinct failure mode so retry prompts can include precise feedback.
/// </summary>
public abstract record ParseResult
{
    private ParseResult() { }

    /// <summary>JSON was valid and deserialized successfully into a <see cref="SupportTicket"/>.</summary>
    public sealed record Success(SupportTicket Ticket) : ParseResult;

    /// <summary>The raw LLM response was null, empty, or whitespace.</summary>
    public sealed record EmptyResponse() : ParseResult;

    /// <summary>
    /// The response was not parseable JSON (<see cref="System.Text.Json.JsonException"/> was thrown).
    /// </summary>
    public sealed record MalformedJson(string Detail) : ParseResult;

    /// <summary>
    /// A field had the wrong JSON value type (e.g., <c>priority</c> was a number instead of a string).
    /// </summary>
    public sealed record WrongDataType(string Detail) : ParseResult;

    /// <summary>Deserialization failed for a reason other than the above cases.</summary>
    public sealed record DeserializationFailed(string Detail) : ParseResult;
}
