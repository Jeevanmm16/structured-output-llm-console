namespace StructuredOutputDemo.Models;

/// <summary>
/// Returned by <see cref="Services.TicketValidator"/> to indicate whether a
/// <see cref="SupportTicket"/> satisfies all required field constraints.
/// </summary>
/// <param name="IsValid">True when all required fields are present and non-empty.</param>
/// <param name="FailureReason">
/// Human-readable explanation of what failed; empty string on success.
/// This message is injected into the LLM retry prompt so the model knows what to fix.
/// </param>
public record ValidationResult(bool IsValid, string FailureReason = "");