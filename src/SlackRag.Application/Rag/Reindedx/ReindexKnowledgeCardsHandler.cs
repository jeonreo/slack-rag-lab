using MediatR;
using SlackRag.Domain.Rag;

namespace SlackRag.Application.Rag.Reindex;

/// <summary>
/// 임베딩 누락 카드에 대해 PII 마스킹 후 임베딩을 재생성한다.
/// </summary>
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
        // 1) 임베딩이 없는 카드만 조회한다.
        var cards = await _repo.GetCardsMissingEmbeddingAsync(ct);

        var updated = 0;

        foreach (var c in cards)
        {
            // 2) 저장/임베딩 전 문제/해결 텍스트를 마스킹한다.
            var redactedProblem = _pii.Redact(c.Problem);
            var redactedSolution = _pii.Redact(c.Solution);

            // 3) Problem+Solution 결합 텍스트로 임베딩을 생성한다.
            var text = $"Problem: {redactedProblem}\nSolution: {redactedSolution}";

            var vec = await _embeddings.CreateEmbeddingAsync(text, ct);

            // 4) 마스킹된 본문과 임베딩을 카드에 반영한다.
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
