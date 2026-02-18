namespace SlackRag.Domain.Rag;

public sealed class RagOptions
{
    public int TopK { get; init; } = 3;
    public double MaxDistance { get; init; } = 1.0;
    public double WeakThreshold { get; init; } = 0.90;
}
