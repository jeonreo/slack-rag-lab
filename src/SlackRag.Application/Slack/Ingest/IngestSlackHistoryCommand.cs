using MediatR;

namespace SlackRag.Application.Slack.Ingest;

public sealed record IngestSlackHistoryCommand(
    string ChannelId,
    int WindowHours = 24,
    int PageSize = 200,
    bool DryRun = false
) : IRequest<IngestSlackHistoryResult>;
public sealed record IngestSlackHistoryResult(int Inserted);
