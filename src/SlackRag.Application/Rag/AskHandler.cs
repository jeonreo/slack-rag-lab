using MediatR;
using SlackRag.Domain.Rag;
using Microsoft.Extensions.Options;

namespace SlackRag.Application.Rag;

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
        var qVec = await _embeddings.CreateEmbeddingAsync(request.Question, ct);

        var topK = _opt.TopK <= 0 ? 3 : Math.Min(_opt.TopK, 20);
        var maxDistance = _opt.MaxDistance <= 0 ? 1.0 : _opt.MaxDistance;
        var weak = _opt.WeakThreshold <= 0 ? 0.90 : _opt.WeakThreshold;

        var hits = (await _search.SearchAsync(qVec, topK, ct)).ToList();

       

        if (hits.Count == 0)
        {
            return new AskResult(
                request.Question,
                "I couldn't find relevant internal context. Can you share more details (service name / environment / error message)?",
                hits
            );
        }

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

        if (contextChunks.Count == 0)
        {
            return new AskResult(
                request.Question,
                "I found matches, but none are reliable enough to answer safely. Can you provide service name, environment, and exact error text?",
                hits
            );
        }

        if (hits[0].Distance > weak)
        {
            return new AskResult(
                request.Question,
                "I found only weakly related context. Which service/component are you working on, and what environment (staging/prod) specifically?",
                hits
            );
        }

        var context = string.Join("\n\n", contextChunks);
        var answer = await _answers.GenerateAsync(request.Question, context, ct);

        return new AskResult(request.Question, answer, hits);
    }
}
