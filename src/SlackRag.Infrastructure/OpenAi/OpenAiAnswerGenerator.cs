using SlackRag.Domain.Rag;
using SlackRag.Infrastructure.Security;

namespace SlackRag.Infrastructure.OpenAi;

/// <summary>
/// OpenAI Chat Completion 기반 답변 생성 구현체다.
/// </summary>
public sealed class OpenAiAnswerGenerator : IAnswerGenerator
{
    public async Task<string> GenerateAsync(string question, string context, CancellationToken ct)
    {
        // 생성 결과에도 2차 마스킹을 적용해 응답 유출 위험을 줄인다.
        var answer = await OpenAiHelper.GenerateAnswerAsync(question, context);
        var redacted = PiiRedactor.Redact(answer);

        return PiiRedactor.LooksLikePii(redacted)
            ? "I can't provide that response safely. Please remove personal or sensitive data and try again."
            : redacted;
    }
}
