namespace SlackRag.Domain.Rag;

public sealed record KnowledgeCardForIndexing(
    int Id,
    string Problem,
    string Solution
);
