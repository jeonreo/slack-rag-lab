namespace SlackRag.Domain.Rag;

/// <summary>
/// 임베딩 재생성 대상 카드 정보를 표현한다.
/// </summary>
public sealed record KnowledgeCardForIndexing(
    int Id,
    string Problem,
    string Solution
);
