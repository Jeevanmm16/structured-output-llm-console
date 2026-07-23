using NUnit.Framework;
using StructuredOutputDemo.Models;
using StructuredOutputDemo.Services;

namespace StructuredOutputDemo.Tests.Tests;

/// <summary>
/// Tests for <see cref="TicketValidator"/> in complete isolation.
/// Constructs <see cref="SupportTicket"/> objects directly — no LLM, no parser.
///
/// Validates that the validator correctly identifies missing or empty required fields.
/// </summary>
[TestFixture]
public class TicketValidatorTests
{
    private TicketValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new TicketValidator();
    }

    // ── Test 5 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 5 — Empty required field: a ticket with an empty summary fails validation.")]
    public void Validate_EmptySummary_ReturnsInvalidResult_WithDescriptiveReason()
    {
        // Arrange — this is exactly what happens after parsing {"category":"Payment","priority":"High"}
        var ticket = new SupportTicket
        {
            Category = "Payment",
            Priority = "High",
            Summary = string.Empty   // missing in the LLM response → default empty string
        };

        // Act
        var result = _validator.Validate(ticket);

        // Assert
        Assert.That(result.IsValid, Is.False,
            "Validation must fail when 'summary' is empty.");
        Assert.That(result.FailureReason, Does.Contain("summary").IgnoreCase,
            "FailureReason must mention 'summary' so the retry prompt is informative.");
    }

    [Test]
    [Description("Bonus — Fully populated ticket passes validation.")]
    public void Validate_AllFieldsPresent_ReturnsValidResult()
    {
        // Arrange
        var ticket = new SupportTicket
        {
            Category = "Payment",
            Priority = "High",
            Summary = "Customer's payment failed multiple times."
        };

        // Act
        var result = _validator.Validate(ticket);

        // Assert
        Assert.That(result.IsValid, Is.True,
            "Validation must succeed when all three fields are non-empty.");
        Assert.That(result.FailureReason, Is.Empty);
    }
}
