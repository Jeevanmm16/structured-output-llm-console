using StructuredOutputDemo.Models;

namespace StructuredOutputDemo.UI;

/// <summary>
/// Handles all console input and output for the application.
/// No business logic lives here — this class is purely presentational.
/// </summary>
public class ConsoleDisplay
{
    private const string Separator = "════════════════════════════════════════════";

    /// <summary>Prints the application header banner.</summary>
    public void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(Separator);
        Console.WriteLine("   SUPPORT TICKET STRUCTURED EXTRACTION");
        Console.WriteLine("   Powered by Ollama + System.Text.Json");
        Console.WriteLine(Separator);
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Prompts the user to enter a support ticket and returns the trimmed input.
    /// Returns <see cref="string.Empty"/> if the user presses Enter without typing.
    /// </summary>
    public string PromptUserInput()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Enter support ticket (or 'exit' to quit): ");
        Console.ResetColor();
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    /// <summary>Echoes the user's input back for clarity.</summary>
    public void PrintInput(string input)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Input : ");
        Console.ResetColor();
        Console.WriteLine(input);
    }

    /// <summary>Displays which attempt number is currently running.</summary>
    public void PrintAttempt(int attempt)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Attempt: {attempt}");
        Console.ResetColor();
    }

    /// <summary>
    /// Displays the successfully extracted ticket in a formatted table,
    /// along with the number of attempts required and a SUCCESS indicator.
    /// </summary>
    public void PrintSuccess(SupportTicket ticket, int attemptCount)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Structured Result:");
        Console.ResetColor();

        Console.WriteLine($"  Category : {ticket.Category}");
        Console.WriteLine($"  Priority : {ticket.Priority}");
        Console.WriteLine($"  Summary  : {ticket.Summary}");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"Validation: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"SUCCESS  (completed in {attemptCount} attempt{(attemptCount == 1 ? "" : "s")})");
        Console.ResetColor();
        Console.WriteLine(new string('─', 44));
        Console.WriteLine();
    }

    /// <summary>Displays a clear error message when extraction fails after all retries.</summary>
    public void PrintFailure(string reason)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Extraction FAILED");
        Console.ResetColor();
        Console.WriteLine($"  Reason: {reason}");
        Console.WriteLine(new string('─', 44));
        Console.WriteLine();
    }

    /// <summary>Displays a validation warning (e.g., empty input) without a full failure block.</summary>
    public void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Warning: {message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}
