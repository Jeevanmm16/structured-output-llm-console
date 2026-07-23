using NUnit.Framework;
using StructuredOutputDemo.Services;
using StructuredOutputDemo.Tests.Fakes;

namespace StructuredOutputDemo.Tests.Tests;

/// <summary>
/// Integration-style tests for <see cref="ExtractionService"/>.
///
/// Each test uses <see cref="FakeLlmClient"/> to control LLM responses precisely.
/// No real Ollama connection is made — all tests are fast and deterministic.
///
/// Key assertions per test:
///   1. Whether the result is success or failure
///   2. The exact number of LLM calls made (verifies retry count)
/// </summary>
[TestFixture]
public class ExtractionServiceTests
{
    // Shared valid JSON — used across multiple tests
    private const string ValidJson = """
        {
            "category": "Payment",
            "priority": "High",
            "summary": "Payment failed multiple times."
        }
        """;

    // Valid JSON for end-to-end test with different summary
    private const string ValidJsonE2E = """
        {
            "category": "Payment",
            "priority": "High",
            "summary": "Customer's credit card payment failed three times and requires urgent assistance."
        }
        """;

    // Invalid JSON — missing priority and summary
    private const string InvalidMissingFieldsJson = """
        { "category": "Payment" }
        """;

    // Malformed JSON — missing closing brace
    private const string MalformedJson = """
        {
            "category": "Payment",
            "priority": "High",
            "summary": "Payment failed."
        """;

    private ExtractionService BuildService(FakeLlmClient fake)
    {
        return new ExtractionService(fake, new JsonParser(), new TicketValidator());
    }

    // ── Test 6 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 6 — Empty user input: rejected before LLM call; CallCount == 0.")]
    public async Task ExtractAsync_EmptyInput_RejectsImmediately_WithoutCallingLlm()
    {
        // Arrange — no responses needed; LLM must never be called
        var fake = new FakeLlmClient(); // empty queue
        var service = BuildService(fake);

        // Act
        var result = await service.ExtractAsync(string.Empty);

        // Assert
        Assert.That(result.IsSuccess, Is.False,
            "Empty input must be rejected.");
        Assert.That(result.AttemptCount, Is.EqualTo(0),
            "AttemptCount must be 0 — no LLM calls should have been made.");
        Assert.That(fake.CallCount, Is.EqualTo(0),
            "FakeLlmClient must not have been called for empty input.");
        Assert.That(result.FailureReason, Is.Not.Empty,
            "A descriptive failure reason must be provided.");
    }

    // ── Test 7 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 7 — Valid first response: succeeds on attempt 1; CallCount == 1.")]
    public async Task ExtractAsync_ValidFirstResponse_SucceedsOnFirstAttempt()
    {
        // Arrange
        var fake = new FakeLlmClient(ValidJson);
        var service = BuildService(fake);

        // Act
        var result = await service.ExtractAsync(
            "My credit card payment failed three times today. I need to complete my order urgently.");

        // Assert
        Assert.That(result.IsSuccess, Is.True, "Extraction must succeed.");
        Assert.That(fake.CallCount, Is.EqualTo(1), "LLM must be called exactly once.");
        Assert.That(result.AttemptCount, Is.EqualTo(1), "AttemptCount must be 1.");
        Assert.That(result.Ticket, Is.Not.Null);
        Assert.That(result.Ticket!.Category, Is.EqualTo("Payment"));
        Assert.That(result.Ticket!.Priority, Is.EqualTo("High"));
        Assert.That(result.Ticket!.Summary, Is.Not.Empty);
    }

    // ── Test 8 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 8 — Invalid first response, valid retry: succeeds on attempt 2; CallCount == 2.")]
    public async Task ExtractAsync_InvalidFirstResponse_ValidSecondResponse_SucceedsOnRetry()
    {
        // Arrange — first response missing priority + summary; second is valid
        var fake = new FakeLlmClient(InvalidMissingFieldsJson, ValidJson);
        var service = BuildService(fake);

        // Act
        var result = await service.ExtractAsync("My payment failed twice.");

        // Assert
        Assert.That(result.IsSuccess, Is.True,
            "Extraction must succeed after one retry.");
        Assert.That(fake.CallCount, Is.EqualTo(2),
            "LLM must be called exactly twice: once for the failure, once for the retry.");
        Assert.That(result.AttemptCount, Is.EqualTo(2),
            "AttemptCount must be 2.");
        Assert.That(result.Ticket, Is.Not.Null);
        Assert.That(result.Ticket!.Category, Is.EqualTo("Payment"));
    }

    // ── Test 9 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 9 — Three invalid responses: returns controlled failure; CallCount == 3.")]
    public async Task ExtractAsync_ThreeInvalidResponses_ReturnsFailure_CallCountIsThree()
    {
        // Arrange — all three attempts return an incomplete ticket
        var fake = new FakeLlmClient(
            InvalidMissingFieldsJson,
            InvalidMissingFieldsJson,
            InvalidMissingFieldsJson);
        var service = BuildService(fake);

        // Act
        var result = await service.ExtractAsync("My account is locked.");

        // Assert
        Assert.That(result.IsSuccess, Is.False,
            "Extraction must fail when all 3 attempts return invalid output.");
        Assert.That(fake.CallCount, Is.EqualTo(3),
            "LLM must be called exactly 3 times — no more, no fewer.");
        Assert.That(result.AttemptCount, Is.EqualTo(3),
            "AttemptCount must be 3.");
        Assert.That(result.FailureReason, Is.Not.Empty,
            "A descriptive failure reason must be returned.");

        // Critical: ensure the guard clause in FakeLlmClient was never triggered
        // (if it was, the test would have thrown before reaching this assertion)
    }

    // ── Test 10 ────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 10 — Malformed first response, valid retry: succeeds on attempt 2; CallCount == 2.")]
    public async Task ExtractAsync_MalformedFirstResponse_ValidSecondResponse_SucceedsOnRetry()
    {
        // Arrange — first response is syntactically broken JSON; second is valid
        var fake = new FakeLlmClient(MalformedJson, ValidJson);
        var service = BuildService(fake);

        // Act
        var result = await service.ExtractAsync("I cannot log in to my account.");

        // Assert
        Assert.That(result.IsSuccess, Is.True,
            "Extraction must succeed after retrying from a malformed JSON response.");
        Assert.That(fake.CallCount, Is.EqualTo(2),
            "LLM must be called exactly twice.");
        Assert.That(result.AttemptCount, Is.EqualTo(2));
        Assert.That(result.Ticket, Is.Not.Null);
        Assert.That(result.Ticket!.Category, Is.EqualTo("Payment"));
        Assert.That(result.Ticket!.Priority, Is.EqualTo("High"));
        Assert.That(result.Ticket!.Summary, Is.Not.Empty);
    }

    // ── End-to-end verification (complete pipeline) ───────────────────────────

    [Test]
    [Description("E2E — Complete pipeline: user prompt → LLM → parse → validate → SupportTicket returned.")]
    public async Task ExtractAsync_FullPipeline_UserPrompt_ProducesValidTicket()
    {
        // Arrange
        var fake = new FakeLlmClient(ValidJsonE2E);
        var service = BuildService(fake);
        const string userPrompt =
            "My credit card payment failed three times today. I need to complete my order urgently.";

        // Act
        var result = await service.ExtractAsync(userPrompt);

        // Assert — every layer of the pipeline
        Assert.That(result.IsSuccess, Is.True, "Full pipeline must succeed.");
        Assert.That(fake.CallCount, Is.EqualTo(1), "No retry should have been needed.");
        Assert.That(result.AttemptCount, Is.EqualTo(1));
        Assert.That(result.Ticket, Is.Not.Null);
        Assert.That(result.Ticket!.Category, Is.EqualTo("Payment"));
        Assert.That(result.Ticket!.Priority, Is.EqualTo("High"));
        Assert.That(result.Ticket!.Summary, Does.Contain("credit card").IgnoreCase
            .Or.Contains("payment").IgnoreCase,
            "Summary should describe the payment failure.");
    }
}
