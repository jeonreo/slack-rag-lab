namespace SlackRag.Domain.Rag;

public interface IKnowledgeCardRepository
{
    Task<IReadOnlyList<KnowledgeCardForIndexing>> GetCardsMissingEmbeddingAsync(CancellationToken ct);

    Task UpdateCardEmbeddingAsync(
        int id,
        string problem,
        string solution,
        float[] embedding,
        CancellationToken ct
    );

    Task<int> InsertKnowledgeCardAsync(
        string problem,
        string solution,
        string sourceUrl,
        CancellationToken ct
    );
    Task EnsureIndexesAsync(CancellationToken ct);
}
