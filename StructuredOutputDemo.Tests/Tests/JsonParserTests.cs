using NUnit.Framework;
using StructuredOutputDemo.Services;

namespace StructuredOutputDemo.Tests.Tests;

/// <summary>
/// Tests for <see cref="JsonParser"/> in complete isolation.
/// No LLM client, no ExtractionService — just raw string in, ParseResult out.
///
/// These tests cover the parsing layer only:
///   - Valid JSON  → ParseResult.Success
///   - Malformed   → ParseResult.MalformedJson (no exception)
///   - Missing field → ParseResult.Success (empty string; validator catches it)
///   - Wrong type  → ParseResult.WrongDataType or MalformedJson (no exception)
/// </summary>
[TestFixture]
public class JsonParserTests
{
    private JsonParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new JsonParser();
    }

    // ── Test 1 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 1 — Valid JSON: parsing and deserialization succeed; all fields populated.")]
    public void TryParse_ValidJson_ReturnsSuccess_WithAllFieldsPopulated()
    {
        // Arrange
        const string validJson = """
            {
                "category": "Payment",
                "priority": "High",
                "summary": "Payment failed multiple times."
            }
            """;

        // Act
        var result = _parser.TryParse(validJson);

        // Assert
        Assert.That(result, Is.InstanceOf<ParseResult.Success>(),
            "Expected ParseResult.Success for well-formed JSON.");

        var success = (ParseResult.Success)result;
        Assert.That(success.Ticket.Category, Is.EqualTo("Payment"));
        Assert.That(success.Ticket.Priority, Is.EqualTo("High"));
        Assert.That(success.Ticket.Summary, Is.EqualTo("Payment failed multiple times."));
    }

    // ── Test 2 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 2 — Malformed JSON: parsing fails gracefully without throwing.")]
    public void TryParse_MalformedJson_ReturnsMalformedJson_DoesNotThrow()
    {
        // Arrange — missing closing brace
        const string malformedJson = """
            {
                "category": "Payment",
                "priority": "High",
                "summary": "Payment failed multiple times."
            """;

        // Act — must not throw
        ParseResult result = null!;
        Assert.DoesNotThrow(() => result = _parser.TryParse(malformedJson),
            "Parser must not throw for malformed JSON.");

        // Assert
        Assert.That(result, Is.InstanceOf<ParseResult.MalformedJson>(),
            "Expected ParseResult.MalformedJson for syntactically broken input.");
    }

    // ── Test 3 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 3 — Missing field: JSON parses successfully but summary is empty string.")]
    public void TryParse_MissingField_ReturnsSuccess_WithEmptySummary()
    {
        // Arrange — 'summary' is omitted entirely
        const string missingFieldJson = """
            {
                "category": "Payment",
                "priority": "High"
            }
            """;

        // Act
        var result = _parser.TryParse(missingFieldJson);

        // Assert — parsing succeeds; the empty field is for the validator to catch
        Assert.That(result, Is.InstanceOf<ParseResult.Success>(),
            "JSON syntax is valid, so parsing must succeed even with a missing field.");

        var success = (ParseResult.Success)result;
        Assert.That(success.Ticket.Category, Is.EqualTo("Payment"));
        Assert.That(success.Ticket.Priority, Is.EqualTo("High"));
        Assert.That(success.Ticket.Summary, Is.Empty,
            "System.Text.Json sets missing string properties to their default (empty string).");
    }

    // ── Test 4 ─────────────────────────────────────────────────────────────────

    [Test]
    [Description("Test 4 — Wrong data type: priority is a number; fails without crashing.")]
    public void TryParse_WrongDataType_ReturnsFailure_DoesNotThrow()
    {
        // Arrange — priority is 123 (integer), not a string
        const string wrongTypeJson = """
            {
                "category": "Payment",
                "priority": 123,
                "summary": "Payment failed multiple times."
            }
            """;

        // Act
        ParseResult result = null!;
        Assert.DoesNotThrow(() => result = _parser.TryParse(wrongTypeJson),
            "Parser must not throw for type-mismatched JSON.");

        // Assert — result must be a parsing failure, not a success
        Assert.That(result,
            Is.InstanceOf<ParseResult.WrongDataType>().Or.InstanceOf<ParseResult.MalformedJson>(),
            "A numeric priority should produce WrongDataType or MalformedJson — never Success.");
    }
}