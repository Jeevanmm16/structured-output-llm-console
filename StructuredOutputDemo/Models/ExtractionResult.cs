namespace StructuredOutputDemo.Models;

/// <summary>
/// Returned by <see cref="Services.ExtractionService"/> after completing the
/// full extraction pipeline (including any retries).
/// </summary>
/// <param name="IsSuccess">True when a valid <see cref="SupportTicket"/> was produced.</param>
/// <param name="Ticket">The extracted ticket; null when <paramref name="IsSuccess"/> is false.</param>
/// <param name="AttemptCount">Total number of LLM calls made (1 on first-try success, up to 3 on failure).</param>
/// <param name="FailureReason">Explanation of why extraction failed; empty string on success.</param>
public record ExtractionResult(
    bool IsSuccess,
    SupportTicket? Ticket,
    int AttemptCount,
    string FailureReason = "");