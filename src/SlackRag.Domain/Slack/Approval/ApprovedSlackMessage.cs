namespace SlackRag.Domain.Slack.Approval;

/// <summary>
/// 승인 리액션으로 채택된 Slack 메시지 식별 정보다.
/// </summary>
public sealed record ApprovedSlackMessage(
    string ChannelId,
    string Ts,
    string Reaction
);
