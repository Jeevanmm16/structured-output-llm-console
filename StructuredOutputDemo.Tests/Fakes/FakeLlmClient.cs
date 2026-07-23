using StructuredOutputDemo.Services.Interfaces;

namespace StructuredOutputDemo.Tests.Fakes;

/// <summary>
/// A deterministic, in-memory fake implementation of <see cref="ILlmClient"/>.
///
/// Responses are pre-loaded into a <see cref="Queue{T}"/> at construction time.
/// Each call to <see cref="GenerateAsync"/> dequeues the next response in order.
///
/// The <see cref="CallCount"/> property lets tests assert the exact number of
/// LLM calls made, which is essential for verifying retry behaviour.
///
/// Guard clause: if the queue is exhausted and an unexpected additional call is made,
/// an <see cref="InvalidOperationException"/> is thrown immediately rather than
/// silently returning empty data. This surfaces retry-loop bugs instantly.
/// </summary>
public class FakeLlmClient : ILlmClient
{
    private readonly Queue<string> _responses;

    /// <summary>Total number of times <see cref="GenerateAsync"/> has been called.</summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Initialises the fake with a fixed sequence of responses.
    /// </summary>
    /// <param name="responses">
    /// Responses returned in order. The first call returns <c>responses[0]</c>,
    /// the second returns <c>responses[1]</c>, and so on.
    /// </param>
    public FakeLlmClient(params string[] responses)
    {
        _responses = new Queue<string>(responses);
    }

    /// <inheritdoc/>
    public Task<string> GenerateAsync(string prompt)
    {
        CallCount++;

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException(
                $"FakeLlmClient received more calls than expected. " +
                $"Call #{CallCount} was made but no more responses were queued. " +
                $"This indicates the retry loop exceeded its maximum attempt count.");
        }

        return Task.FromResult(_responses.Dequeue());
    }
}