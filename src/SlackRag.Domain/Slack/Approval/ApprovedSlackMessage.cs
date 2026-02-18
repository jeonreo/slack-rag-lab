namespace SlackRag.Domain.Slack.Approval;

public sealed record ApprovedSlackMessage(
    string ChannelId,
    string Ts,
    string Reaction
);
