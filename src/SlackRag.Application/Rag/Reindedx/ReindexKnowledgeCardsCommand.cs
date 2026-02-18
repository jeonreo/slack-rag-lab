using MediatR;

namespace SlackRag.Application.Rag.Reindex;

/// <summary>
/// 임베딩 누락 카드 재색인을 요청한다.
/// </summary>
public sealed record ReindexKnowledgeCardsCommand : IRequest<ReindexKnowledgeCardsResult>;

/// <summary>
/// 재색인 완료 건수를 반환한다.
/// </summary>
public sealed record ReindexKnowledgeCardsResult(int Updated);
