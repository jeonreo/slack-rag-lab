namespace SlackRag.Domain.Slack;

public sealed record SlackMessage(
    string Ts,
    string Text,
    DateTimeOffset Timestamp
);
