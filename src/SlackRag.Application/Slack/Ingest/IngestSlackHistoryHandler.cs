using MediatR;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;

namespace SlackRag.Application.Slack.Ingest;

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
        var hours = request.WindowHours <= 0 ? 24 : Math.Min(request.WindowHours, 168);
        var oldest = DateTimeOffset.UtcNow.AddHours(-hours);

        var pageSize = request.PageSize <= 0 ? 200 : Math.Min(request.PageSize, 200);

        var messages = await _slack.GetMessagesAsync(request.ChannelId, pageSize, oldest, ct);

        var inserted = 0;

        foreach (var m in messages)
        {
            var text = m.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            var problem = _pii.Redact(text);
            var solution = "TBD";
            var sourceUrl = $"slack://{request.ChannelId}/{m.Ts}";

            if (request.DryRun) { inserted++; continue; }
            Console.WriteLine($"insert candidate sourceUrl={sourceUrl}");
           var affected = await _repo.InsertKnowledgeCardAsync(problem, solution, sourceUrl, ct);
            inserted += affected;
        }

        return new IngestSlackHistoryResult(inserted);
    }
}
