namespace SlackRag.Domain.Rag;

/// <summary>
/// 검색된 컨텍스트를 기반으로 최종 답변을 생성하는 도메인 계약이다.
/// </summary>
public interface IAnswerGenerator
{
    /// <summary>
    /// 질문과 컨텍스트를 받아 LLM 기반 답변 텍스트를 생성한다.
    /// </summary>
    Task<string> GenerateAsync(string question, string context, CancellationToken ct);
}
