namespace SlackRag.Domain.Rag;

/// <summary>
/// 지식카드 저장소(주로 PostgreSQL) 접근을 추상화하는 도메인 계약이다.
/// </summary>
public interface IKnowledgeCardRepository
{
    /// <summary>
    /// 임베딩이 없는 카드 목록을 조회한다.
    /// </summary>
    Task<IReadOnlyList<KnowledgeCardForIndexing>> GetCardsMissingEmbeddingAsync(CancellationToken ct);

    /// <summary>
    /// 카드 본문(PII 마스킹 반영)과 임베딩을 갱신한다.
    /// </summary>
    Task UpdateCardEmbeddingAsync(
        int id,
        string problem,
        string solution,
        float[] embedding,
        CancellationToken ct
    );

    /// <summary>
    /// 신규 지식카드를 저장한다. 중복 URL은 저장소 정책에 따라 무시될 수 있다.
    /// </summary>
    Task<int> InsertKnowledgeCardAsync(
        string problem,
        string solution,
        string sourceUrl,
        CancellationToken ct
    );

    /// <summary>
    /// 서비스에 필요한 DB 인덱스를 보장한다.
    /// </summary>
    Task EnsureIndexesAsync(CancellationToken ct);
}
