using MediatR;
using SlackRag.Domain.Rag;

namespace SlackRag.Application.Rag;

/// <summary>
/// 사용자 질문에 대한 RAG 응답 생성을 요청한다.
/// </summary>
public sealed record AskQuery(string Question) : IRequest<AskResult>;

/// <summary>
/// 질문, 생성된 답변, 검색 hit 목록을 함께 반환한다.
/// </summary>
public sealed record AskResult(string Question, string Answer, IReadOnlyList<KnowledgeCardHit> Hits);
