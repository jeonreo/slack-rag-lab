using MediatR;

namespace SlackRag.Application.Slack.Ingest;

/// <summary>
/// Slack 히스토리를 수집해 지식카드로 적재하는 요청이다.
/// </summary>
public sealed record IngestSlackHistoryCommand(
    string ChannelId,
    int WindowHours = 24,
    int PageSize = 200,
    bool DryRun = false
) : IRequest<IngestSlackHistoryResult>;

/// <summary>
/// 적재(또는 드라이런 후보) 건수를 반환한다.
/// </summary>
public sealed record IngestSlackHistoryResult(int Inserted);
