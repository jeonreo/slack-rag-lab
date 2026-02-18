namespace SlackRag.Domain.Slack;

/// <summary>
/// Slack 메시지 도메인 모델이다.
/// </summary>
public sealed record SlackMessage(
    string Ts,
    string Text,
    DateTimeOffset Timestamp
);
