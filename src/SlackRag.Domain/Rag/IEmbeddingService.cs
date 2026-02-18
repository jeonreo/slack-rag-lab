namespace SlackRag.Domain.Rag;

/// <summary>
/// 텍스트를 벡터 임베딩으로 변환하는 도메인 계약이다.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// 입력 텍스트를 임베딩 벡터로 변환한다.
    /// </summary>
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct);
}
