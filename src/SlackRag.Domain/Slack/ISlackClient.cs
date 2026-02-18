namespace SlackRag.Domain.Slack;

/// <summary>
/// Slack 메시지 조회 기능을 제공하는 도메인 계약이다.
/// </summary>
public interface ISlackClient
{
    /// <summary>
    /// 지정 채널의 이력 메시지를 페이지 단위로 수집한다.
    /// </summary>
    Task<IReadOnlyList<SlackMessage>> GetMessagesAsync(
        string channelId,
        int pageSize,
        DateTimeOffset oldest,
        CancellationToken ct
    );

    /// <summary>
    /// 채널/타임스탬프로 단건 메시지를 조회한다.
    /// </summary>
    Task<SlackMessage?> GetMessageAsync(
    string channelId,
    string ts,
    CancellationToken ct
    );
}
