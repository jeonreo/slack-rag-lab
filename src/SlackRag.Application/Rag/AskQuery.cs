using MediatR;
using SlackRag.Domain.Rag;

namespace SlackRag.Application.Rag;

public sealed record AskQuery(string Question) : IRequest<AskResult>;

public sealed record AskResult(string Question, string Answer, IReadOnlyList<KnowledgeCardHit> Hits);
