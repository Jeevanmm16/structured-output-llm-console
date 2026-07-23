using StructuredOutputDemo.Models;

namespace StructuredOutputDemo.Services;

/// <summary>
/// Validates that a successfully parsed <see cref="SupportTicket"/> satisfies
/// the application's required-field schema.
///
/// This is intentionally separate from <see cref="JsonParser"/>:
///   - Parser: "Is the JSON syntactically correct and correctly typed?"
///   - Validator: "Does the resulting object contain all required non-empty fields?"
///
/// This separation demonstrates that valid JSON does not automatically equal valid structured output.
/// </summary>
public class TicketValidator
{
    /// <summary>
    /// Validates that <paramref name="ticket"/> has non-empty values for all required fields.
    /// </summary>
    /// <param name="ticket">The ticket to validate.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> with <c>IsValid = true</c> on success,
    /// or a descriptive <c>FailureReason</c> on failure (used to build retry prompts).
    /// </returns>
    public ValidationResult Validate(SupportTicket ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.Category))
            return new ValidationResult(false, "The 'category' field is missing or empty.");

        if (string.IsNullOrWhiteSpace(ticket.Priority))
            return new ValidationResult(false, "The 'priority' field is missing or empty.");

        if (string.IsNullOrWhiteSpace(ticket.Summary))
            return new ValidationResult(false, "The 'summary' field is missing or empty.");

        return new ValidationResult(true);
    }
}
