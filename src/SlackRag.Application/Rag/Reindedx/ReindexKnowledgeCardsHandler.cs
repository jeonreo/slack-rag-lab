using MediatR;
using SlackRag.Domain.Rag;

namespace SlackRag.Application.Rag.Reindex;

public sealed class ReindexKnowledgeCardsHandler
    : IRequestHandler<ReindexKnowledgeCardsCommand, ReindexKnowledgeCardsResult>
{
    private readonly IKnowledgeCardRepository _repo;
    private readonly IEmbeddingService _embeddings;
    private readonly IPiiRedactor _pii;

    public ReindexKnowledgeCardsHandler(
        IKnowledgeCardRepository repo,
        IEmbeddingService embeddings,
        IPiiRedactor pii)
    {
        _repo = repo;
        _embeddings = embeddings;
        _pii = pii;
    }

    public async Task<ReindexKnowledgeCardsResult> Handle(ReindexKnowledgeCardsCommand request, CancellationToken ct)
    {
        var cards = await _repo.GetCardsMissingEmbeddingAsync(ct);

        var updated = 0;

        foreach (var c in cards)
        {
            var redactedProblem = _pii.Redact(c.Problem);
            var redactedSolution = _pii.Redact(c.Solution);

            var text = $"Problem: {redactedProblem}\nSolution: {redactedSolution}";

            var vec = await _embeddings.CreateEmbeddingAsync(text, ct);

            await _repo.UpdateCardEmbeddingAsync(
                c.Id,
                redactedProblem,
                redactedSolution,
                vec,
                ct
            );

            updated++;
        }

        return new ReindexKnowledgeCardsResult(updated);
    }
}
