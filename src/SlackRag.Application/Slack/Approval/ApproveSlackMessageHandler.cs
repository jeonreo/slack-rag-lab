using MediatR;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;
using SlackRag.Domain.Slack.Approval;

namespace SlackRag.Application.Slack.Approval;

/// <summary>
/// 승인 대상 Slack 메시지를 단건 조회해 카드 저장소에 반영한다.
/// </summary>
public sealed class ApproveSlackMessageHandler
    : IRequestHandler<ApproveSlackMessageCommand, ApproveSlackMessageResult>
{
    private readonly ISlackClient _slack;
    private readonly IKnowledgeCardRepository _repo;
    private readonly IPiiRedactor _pii;

    public ApproveSlackMessageHandler(ISlackClient slack, IKnowledgeCardRepository repo, IPiiRedactor pii)
    {
        _slack = slack;
        _repo = repo;
        _pii = pii;
    }

    public async Task<ApproveSlackMessageResult> Handle(ApproveSlackMessageCommand request, CancellationToken ct)
    {
        // 1) 승인된 ts 기준으로 원본 메시지를 조회한다.
        var msg = await _slack.GetMessageAsync(request.ChannelId, request.Ts, ct);
        if (msg is null)
            return new ApproveSlackMessageResult(false, "message_not_found");

        var text = (msg.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            return new ApproveSlackMessageResult(false, "empty_text");

        // 2) 카드 저장 전 메시지 본문을 마스킹한다.
        var problem = _pii.Redact(text);
        var solution = "TBD";

        // Slack permalink 형식으로 source URL 키를 생성한다.
        var tsKey = request.Ts.Replace(".", "");
        var sourceUrl = $"https://slack.com/archives/{request.ChannelId}/p{tsKey}";

        // 3) source_url unique 제약으로 중복 저장을 방지한다.
        var affected = await _repo.InsertKnowledgeCardAsync(problem, solution, sourceUrl, ct);

        return affected == 1
            ? new ApproveSlackMessageResult(true)
            : new ApproveSlackMessageResult(false, "duplicate");
    }
}
