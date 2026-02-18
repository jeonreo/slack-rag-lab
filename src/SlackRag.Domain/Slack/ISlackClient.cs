namespace SlackRag.Domain.Slack;

public interface ISlackClient
{
    Task<IReadOnlyList<SlackMessage>> GetMessagesAsync(
        string channelId,
        int pageSize,
        DateTimeOffset oldest,
        CancellationToken ct
    );
    Task<SlackMessage?> GetMessageAsync(
    string channelId,
    string ts,
    CancellationToken ct
    );
}
