using MediatR;

namespace SlackRag.Application.Slack.Approval;

/// <summary>
/// Slack 승인 리액션이 달린 메시지를 지식카드로 채택하는 요청이다.
/// </summary>
public sealed record ApproveSlackMessageCommand(
    string ChannelId,
    string Ts,
    string Reaction
) : IRequest<ApproveSlackMessageResult>;

/// <summary>
/// 채택 결과와 실패 사유(선택)를 반환한다.
/// </summary>
public sealed record ApproveSlackMessageResult(bool Inserted, string? Reason = null);
