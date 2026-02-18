using SlackRag.Domain.Rag;
using SlackRag.Infrastructure.Security;

namespace SlackRag.Infrastructure.OpenAi;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct)
    {
        var redacted = PiiRedactor.Redact(text);
        return await OpenAiHelper.CreateEmbeddingAsync(redacted);
    }
}
