namespace SlackRag.Domain.Rag;

/// <summary>
/// RAG 검색/응답 품질을 제어하는 설정값 모음이다.
/// </summary>
public sealed class RagOptions
{
    /// <summary>
    /// 벡터 검색 상위 후보 개수.
    /// </summary>
    public int TopK { get; init; } = 3;

    /// <summary>
    /// 컨텍스트로 채택할 최대 거리 임계값.
    /// </summary>
    public double MaxDistance { get; init; } = 1.0;

    /// <summary>
    /// 최상위 hit가 이 값보다 크면 약한 매칭으로 간주한다.
    /// </summary>
    public double WeakThreshold { get; init; } = 0.90;
}
