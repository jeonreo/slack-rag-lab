using SlackRag.Domain.Rag;
using SlackRag.Infrastructure.Security;

namespace SlackRag.Infrastructure.OpenAi;

/// <summary>
/// OpenAI Embedding API 기반 임베딩 생성 구현체다.
/// </summary>
public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct)
    {
        // 임베딩 호출 전에 입력 텍스트를 마스킹한다.
        var redacted = PiiRedactor.Redact(text);
        return await OpenAiHelper.CreateEmbeddingAsync(redacted);
    }
}
