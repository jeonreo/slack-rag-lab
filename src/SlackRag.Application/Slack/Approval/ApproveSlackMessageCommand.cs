using MediatR;

namespace SlackRag.Application.Slack.Approval;

public sealed record ApproveSlackMessageCommand(
    string ChannelId,
    string Ts,
    string Reaction
) : IRequest<ApproveSlackMessageResult>;

public sealed record ApproveSlackMessageResult(bool Inserted, string? Reason = null);
