using MediatR;

namespace SlackRag.Application.Rag.Reindex;

public sealed record ReindexKnowledgeCardsCommand : IRequest<ReindexKnowledgeCardsResult>;

public sealed record ReindexKnowledgeCardsResult(int Updated);
