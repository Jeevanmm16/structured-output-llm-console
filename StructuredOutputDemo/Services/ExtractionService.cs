using StructuredOutputDemo.Models;
using StructuredOutputDemo.Services.Interfaces;

namespace StructuredOutputDemo.Services;

/// <summary>
/// Orchestrates the complete support-ticket extraction pipeline:
///   User Input → Validate Input → LLM → Parse JSON → Validate Ticket → (Retry if needed)
///
/// Retry behaviour:
///   - Maximum 3 attempts total (configurable via <see cref="MaxAttempts"/>).
///   - Each retry prompt explains exactly what went wrong on the previous attempt.
///   - If all attempts are exhausted, a controlled failure result is returned (no exception thrown).
///   - Empty user input is rejected immediately; the LLM is never called.
/// </summary>
public class ExtractionService
{
    private const int MaxAttempts = 3;

    private readonly ILlmClient _llmClient;
    private readonly JsonParser _jsonParser;
    private readonly TicketValidator _validator;

    public ExtractionService(
        ILlmClient llmClient,
        JsonParser jsonParser,
        TicketValidator validator)
    {
        _llmClient = llmClient;
        _jsonParser = jsonParser;
        _validator = validator;
    }

    /// <summary>
    /// Runs the extraction pipeline for the supplied user input.
    /// </summary>
    /// <param name="userInput">Free-form support ticket text entered by the user.</param>
    /// <returns>An <see cref="ExtractionResult"/> describing success or failure.</returns>
    public async Task<ExtractionResult> ExtractAsync(string userInput)
    {
        // Guard: reject empty input before making any LLM call
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return new ExtractionResult(
                IsSuccess: false,
                Ticket: null,
                AttemptCount: 0,
                FailureReason: "User input cannot be empty. Please enter a support ticket description.");
        }

        string? lastError = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Build prompt: first attempt uses base prompt; retries include error feedback
            string prompt = attempt == 1
                ? BuildBasePrompt(userInput)
                : BuildRetryPrompt(userInput, lastError!, attempt);

            // --- LLM Call ---
            string rawResponse = await _llmClient.GenerateAsync(prompt);

            // --- Parse ---
            ParseResult parseResult = _jsonParser.TryParse(rawResponse);

            if (parseResult is not ParseResult.Success successParse)
            {
                lastError = DescribeParseFailure(parseResult);
                continue;
            }

            // --- Validate ---
            ValidationResult validation = _validator.Validate(successParse.Ticket);

            if (!validation.IsValid)
            {
                lastError = validation.FailureReason;
                continue;
            }

            // --- Success ---
            return new ExtractionResult(
                IsSuccess: true,
                Ticket: successParse.Ticket,
                AttemptCount: attempt);
        }

        return new ExtractionResult(
            IsSuccess: false,
            Ticket: null,
            AttemptCount: MaxAttempts,
            FailureReason: $"Extraction failed after {MaxAttempts} attempts. Last error: {lastError}");
    }

    // -------------------------------------------------------------------------
    // Prompt builders
    // -------------------------------------------------------------------------

    private static string BuildBasePrompt(string userInput) =>
        $$"""
        You are a support ticket classifier.

        Analyze the following support ticket text and extract structured information.

        Return ONLY a valid JSON object with exactly these three fields:
        {
          "category": "<string>",
          "priority": "<string>",
          "summary": "<string>"
        }

        Rules:
        - category: classify the ticket type (e.g. Payment, Technical, Account, Billing, Shipping)
        - priority: assess urgency as one of: Low, Medium, High, Critical
        - summary: a concise one-sentence description of the issue
        - Do NOT include markdown code fences (``` or ```json)
        - Do NOT include any explanatory text before or after the JSON
        - Return ONLY the raw JSON object

        Support ticket:
        {{userInput}}
        """;

    private static string BuildRetryPrompt(string userInput, string lastError, int attempt) =>
        $$"""
        Your previous response was invalid. This is attempt {{attempt}} of 3.

        Problem with your last response:
        {{lastError}}

        You must return ONLY a valid JSON object with exactly these three fields:
        {
          "category": "<string>",
          "priority": "<string>",
          "summary": "<string>"
        }

        Rules:
        - All three fields are required and must be non-empty strings
        - category: e.g. Payment, Technical, Account, Billing, Shipping
        - priority: one of Low, Medium, High, Critical
        - summary: a concise one-sentence description
        - Do NOT include markdown code fences
        - Do NOT include any text outside the JSON object
        - Return ONLY the raw JSON object

        Support ticket:
        {{userInput}}
        """;

    // -------------------------------------------------------------------------
    // Parse failure → human-readable description for retry prompts
    // -------------------------------------------------------------------------

    private static string DescribeParseFailure(ParseResult result) => result switch
    {
        ParseResult.EmptyResponse =>
            "Your response was empty. Return a JSON object as instructed.",

        ParseResult.MalformedJson { Detail: var detail } =>
            $"Your response was not valid JSON (syntax error). Detail: {detail}",

        ParseResult.WrongDataType { Detail: var detail } =>
            $"A field had the wrong data type. All fields must be strings. Detail: {detail}",

        ParseResult.DeserializationFailed { Detail: var detail } =>
            $"Deserialization failed. Detail: {detail}",

        _ => "Unknown parse error. Return a valid JSON object."
    };
}
