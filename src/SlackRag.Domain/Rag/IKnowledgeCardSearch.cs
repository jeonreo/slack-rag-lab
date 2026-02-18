namespace SlackRag.Domain.Rag;

/// <summary>
/// 질의 임베딩을 기준으로 유사 지식카드를 검색하는 도메인 계약이다.
/// </summary>
public interface IKnowledgeCardSearch
{
    /// <summary>
    /// 벡터 거리 기반으로 상위 카드들을 반환한다.
    /// </summary>
    Task<IReadOnlyList<KnowledgeCardHit>> SearchAsync(
        float[] embedding,
        int limit,
        CancellationToken ct
    );
}
