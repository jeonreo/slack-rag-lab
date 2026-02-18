using System.Globalization;
using System.Text.Json;
using SlackRag.Domain.Slack;

namespace SlackRag.Infrastructure.Slack;

/// <summary>
/// Slack Web API를 호출해 메시지 이력/단건 조회를 수행한다.
/// </summary>
public sealed class SlackWebApiClient : ISlackClient
{
    private readonly HttpClient _http;

    public SlackWebApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<SlackMessage>> GetMessagesAsync(
        string channelId,
        int pageSize,
        DateTimeOffset oldest,
        CancellationToken ct
    )
    {
        var results = new List<SlackMessage>();
        var oldestSeconds = oldest.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        string? cursor = null;

        while (true)
        {
            // conversations.history를 페이지네이션(cursor) 방식으로 순회한다.
            var url =
                $"https://slack.com/api/conversations.history?channel={channelId}" +
                $"&limit={pageSize}&oldest={oldestSeconds}" +
                (string.IsNullOrWhiteSpace(cursor) ? "" : $"&cursor={Uri.EscapeDataString(cursor)}");

            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
            {
                var err = root.TryGetProperty("error", out var errEl) ? errEl.GetString() : "unknown_error";
                throw new InvalidOperationException($"Slack API failed: {err}");
            }

            if (root.TryGetProperty("messages", out var msgs))
            {
                foreach (var m in msgs.EnumerateArray())
                {
                    var text = m.TryGetProperty("text", out var tEl) ? (tEl.GetString() ?? "") : "";
                    var ts = m.TryGetProperty("ts", out var tsEl) ? (tsEl.GetString() ?? "") : "";

                    if (string.IsNullOrWhiteSpace(text)) continue;
                    if (string.IsNullOrWhiteSpace(ts)) continue;

                    if (!TryParseSlackTs(ts, out var tsTime)) continue;

                    // 도메인 모델로 정규화해 누적한다.
                    results.Add(new SlackMessage(ts, text, tsTime));
                }
            }

            cursor = null;
            if (root.TryGetProperty("response_metadata", out var meta) &&
                meta.TryGetProperty("next_cursor", out var next) &&
                !string.IsNullOrWhiteSpace(next.GetString()))
            {
                cursor = next.GetString();
            }

            if (string.IsNullOrWhiteSpace(cursor))
                break;
        }

        return results;
    }

    public async Task<SlackMessage?> GetMessageAsync(string channelId, string ts, CancellationToken ct)
    {
        // conversations.replies에서 지정 ts에 해당하는 메시지를 단건 추출한다.
        var url = $"https://slack.com/api/conversations.replies?channel={channelId}&ts={Uri.EscapeDataString(ts)}&limit=1";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
        {
            var err = root.TryGetProperty("error", out var errEl) ? errEl.GetString() : "unknown_error";
            throw new InvalidOperationException($"Slack API failed: {err}");
        }

        if (!root.TryGetProperty("messages", out var msgs)) return null;

        foreach (var m in msgs.EnumerateArray())
        {
            var text = m.TryGetProperty("text", out var tEl) ? (tEl.GetString() ?? "") : "";
            var mTs = m.TryGetProperty("ts", out var tsEl) ? (tsEl.GetString() ?? "") : "";

            if (string.IsNullOrWhiteSpace(mTs)) continue;
            if (!string.Equals(mTs, ts, StringComparison.Ordinal)) continue;

            if (!TryParseSlackTs(mTs, out var dto)) dto = DateTimeOffset.UtcNow;

            return new SlackMessage(mTs, text, dto);
        }

        return null;
    }

    private static bool TryParseSlackTs(string ts, out DateTimeOffset dto)
    {
        // Slack ts(예: 1700000000.123456)에서 초 단위를 파싱한다.
        dto = default;

        var dot = ts.IndexOf('.');
        var secondsPart = dot >= 0 ? ts[..dot] : ts;

        if (!long.TryParse(secondsPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            return false;

        dto = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }
}
