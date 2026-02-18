using MediatR;
using SlackRag.Domain.Rag;
using Microsoft.Extensions.Options;

namespace SlackRag.Application.Rag;

/// <summary>
/// 질문 임베딩 생성, 벡터 검색, 컨텍스트 검증, 답변 생성을 오케스트레이션한다.
/// </summary>
public sealed class AskHandler : IRequestHandler<AskQuery, AskResult>
{
    private readonly IEmbeddingService _embeddings;
    private readonly IKnowledgeCardSearch _search;
    private readonly IAnswerGenerator _answers;

    private readonly RagOptions _opt;

    public AskHandler(
        IEmbeddingService embeddings,
        IKnowledgeCardSearch search,
        IAnswerGenerator answers,
        IOptions<RagOptions> opt)
    {
        _embeddings = embeddings;
        _search = search;
        _answers = answers;
        _opt = opt.Value;
    }


    public async Task<AskResult> Handle(AskQuery request, CancellationToken ct)
    {
        // 1) 질문을 임베딩 벡터로 변환한다.
        var qVec = await _embeddings.CreateEmbeddingAsync(request.Question, ct);

        // 2) 설정값을 안전 범위로 보정한다.
        var topK = _opt.TopK <= 0 ? 3 : Math.Min(_opt.TopK, 20);
        var maxDistance = _opt.MaxDistance <= 0 ? 1.0 : _opt.MaxDistance;
        var weak = _opt.WeakThreshold <= 0 ? 0.90 : _opt.WeakThreshold;

        // 3) 유사 카드 후보를 검색한다.
        var hits = (await _search.SearchAsync(qVec, topK, ct)).ToList();

        // 4) 후보가 없으면 추측 대신 추가 정보 요청으로 폴백한다.
        if (hits.Count == 0)
        {
            return new AskResult(
                request.Question,
                "I couldn't find relevant internal context. Can you share more details (service name / environment / error message)?",
                hits
            );
        }

        // 5) 거리 임계값 내 카드만 LLM 컨텍스트로 구성한다.
        var contextChunks = new List<string>();
        foreach (var h in hits)
        {
            if (h.Distance <= maxDistance)
            {
                contextChunks.Add(
                    $"[Card {h.Id}]\nProblem: {h.Problem}\nSolution: {h.Solution}\nSource: {h.SourceUrl}\nDistance: {h.Distance:0.####}"
                );
            }
        }

        // 6) 신뢰 가능한 컨텍스트가 없으면 안전하게 추가 질문을 반환한다.
        if (contextChunks.Count == 0)
        {
            return new AskResult(
                request.Question,
                "I found matches, but none are reliable enough to answer safely. Can you provide service name, environment, and exact error text?",
                hits
            );
        }

        // 7) 최상위 hit가 너무 약하면 추측 답변을 막고 보강 질문을 반환한다.
        if (hits[0].Distance > weak)
        {
            return new AskResult(
                request.Question,
                "I found only weakly related context. Which service/component are you working on, and what environment (staging/prod) specifically?",
                hits
            );
        }

        // 8) 검증된 컨텍스트만 전달해 최종 답변을 생성한다.
        var context = string.Join("\n\n", contextChunks);
        var answer = await _answers.GenerateAsync(request.Question, context, ct);

        return new AskResult(request.Question, answer, hits);
    }
}
