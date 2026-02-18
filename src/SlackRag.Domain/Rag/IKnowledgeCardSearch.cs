namespace SlackRag.Domain.Rag;

public interface IKnowledgeCardSearch
{
    Task<IReadOnlyList<KnowledgeCardHit>> SearchAsync(
        float[] embedding,
        int limit,
        CancellationToken ct
    );
}
