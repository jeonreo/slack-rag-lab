namespace SlackRag.Domain.Rag;

/// <summary>
/// 벡터 검색 결과 1건을 표현한다.
/// </summary>
public sealed record KnowledgeCardHit(
    int Id,
    string Problem,
    string Solution,
    string? SourceUrl,
    double Distance
);