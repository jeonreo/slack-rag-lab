using MediatR;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;

namespace SlackRag.Application.Slack.Ingest;

/// <summary>
/// Slack 메시지 이력을 읽어 PII 마스킹 후 지식카드 저장소에 적재한다.
/// </summary>
public sealed class IngestSlackHistoryHandler
    : IRequestHandler<IngestSlackHistoryCommand, IngestSlackHistoryResult>
{
    private readonly ISlackClient _slack;
    private readonly IKnowledgeCardRepository _repo;
    private readonly IPiiRedactor _pii;

    public IngestSlackHistoryHandler(ISlackClient slack, IKnowledgeCardRepository repo, IPiiRedactor pii)
    {
        _slack = slack;
        _repo = repo;
        _pii = pii;
    }

    public async Task<IngestSlackHistoryResult> Handle(IngestSlackHistoryCommand request, CancellationToken ct)
    {
        // 과도한 조회를 막기 위해 시간 창을 24~168시간 범위로 보정한다.
        var hours = request.WindowHours <= 0 ? 24 : Math.Min(request.WindowHours, 168);
        var oldest = DateTimeOffset.UtcNow.AddHours(-hours);

        // Slack API 제한을 고려해 page size를 최대 200으로 제한한다.
        var pageSize = request.PageSize <= 0 ? 200 : Math.Min(request.PageSize, 200);

        var messages = await _slack.GetMessagesAsync(request.ChannelId, pageSize, oldest, ct);

        var inserted = 0;

        foreach (var m in messages)
        {
            var text = m.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            // 저장 전 원문을 마스킹해 PII 노출 가능성을 줄인다.
            var problem = _pii.Redact(text);
            var solution = "TBD";
            var sourceUrl = $"slack://{request.ChannelId}/{m.Ts}";

            // DryRun 모드에서는 실제 저장 없이 건수만 집계한다.
            if (request.DryRun) { inserted++; continue; }
            Console.WriteLine($"insert candidate sourceUrl={sourceUrl}");
            var affected = await _repo.InsertKnowledgeCardAsync(problem, solution, sourceUrl, ct);
            inserted += affected;
        }

        return new IngestSlackHistoryResult(inserted);
    }
}
