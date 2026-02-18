namespace SlackRag.Domain.Rag;

public interface IEmbeddingService
{
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct);
}
