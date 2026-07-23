using StructuredOutputDemo.Services;
using StructuredOutputDemo.Services.Interfaces;
using StructuredOutputDemo.UI;

// ── Dependency wiring (manual, no DI framework) ──────────────────────────────
HttpClient httpClient = new();
ILlmClient llmClient = new OllamaClient(httpClient);
JsonParser jsonParser = new();
TicketValidator validator = new();
ExtractionService extractionService = new(llmClient, jsonParser, validator);
ConsoleDisplay display = new();

// ── Application loop ──────────────────────────────────────────────────────────
display.PrintHeader();

while (true)
{
    string? userInput = display.PromptUserInput();

    // null = EOF: stdin is not interactive (e.g. VS run button, piped input)
    // Show a clear message and exit rather than looping endlessly.
    if (userInput is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine("  ERROR: No interactive terminal detected.");
        Console.WriteLine("  Please run this application from a terminal:");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("    dotnet run --project StructuredOutputDemo");
        Console.WriteLine();
        Console.ResetColor();
        break;
    }

    if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Goodbye.");
        break;
    }

    if (string.IsNullOrWhiteSpace(userInput))
    {
        display.PrintWarning("Input cannot be empty. Please enter a support ticket description.");
        continue;
    }

    display.PrintInput(userInput);

    try
    {
        var result = await extractionService.ExtractAsync(userInput);

        if (result.IsSuccess)
            display.PrintSuccess(result.Ticket!, result.AttemptCount);
        else
            display.PrintFailure(result.FailureReason);
    }
    catch (HttpRequestException ex)
    {
        display.PrintFailure(
            $"Cannot connect to Ollama. Is it running? Run: ollama serve\nDetail: {ex.Message}");
    }
    catch (Exception ex)
    {
        display.PrintFailure($"Unexpected error: {ex.Message}");
    }
}
