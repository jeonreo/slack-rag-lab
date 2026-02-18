using MediatR;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;
using SlackRag.Domain.Slack.Approval;

namespace SlackRag.Application.Slack.Approval;

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
        var msg = await _slack.GetMessageAsync(request.ChannelId, request.Ts, ct);
        if (msg is null)
            return new ApproveSlackMessageResult(false, "message_not_found");

        var text = (msg.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            return new ApproveSlackMessageResult(false, "empty_text");

        var problem = _pii.Redact(text);
        var solution = "TBD";

        // permalink로 표준화 추천
        var tsKey = request.Ts.Replace(".", "");
        var sourceUrl = $"https://slack.com/archives/{request.ChannelId}/p{tsKey}";

        var affected = await _repo.InsertKnowledgeCardAsync(problem, solution, sourceUrl, ct);

        return affected == 1
            ? new ApproveSlackMessageResult(true)
            : new ApproveSlackMessageResult(false, "duplicate");
    }
}
