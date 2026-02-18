namespace SlackRag.Api.Contracts;

/// <summary>
/// /ask 요청 본문 DTO다.
/// </summary>
public sealed record AskRequest(string Question);
