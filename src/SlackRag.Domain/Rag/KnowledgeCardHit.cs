namespace SlackRag.Domain.Rag;

public sealed record KnowledgeCardHit(
    int Id,
    string Problem,
    string Solution,
    string? SourceUrl,
    double Distance
);
