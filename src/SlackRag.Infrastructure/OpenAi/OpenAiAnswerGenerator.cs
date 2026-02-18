using SlackRag.Domain.Rag;
using SlackRag.Infrastructure.Security;

namespace SlackRag.Infrastructure.OpenAi;

public sealed class OpenAiAnswerGenerator : IAnswerGenerator
{
    public async Task<string> GenerateAsync(string question, string context, CancellationToken ct)
    {
        var answer = await OpenAiHelper.GenerateAnswerAsync(question, context);
        var redacted = PiiRedactor.Redact(answer);

        return PiiRedactor.LooksLikePii(redacted)
            ? "I can't provide that response safely. Please remove personal or sensitive data and try again."
            : redacted;
    }
}
