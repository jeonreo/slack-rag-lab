namespace SlackRag.Domain.Rag;

/// <summary>
/// 민감정보(PII) 마스킹 처리를 위한 도메인 계약이다.
/// </summary>
public interface IPiiRedactor
{
    /// <summary>
    /// 입력 문자열에서 민감정보를 마스킹한다.
    /// </summary>
    string Redact(string input);
}
