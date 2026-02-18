using System.Security.Cryptography;
using System.Text;

namespace SlackRag.Api.Slack;

public static class SlackRequestVerifier
{
    public static bool Verify(string signingSecret, string timestamp, byte[] rawBody, string slackSignature, TimeSpan tolerance)
    {
        if (string.IsNullOrWhiteSpace(signingSecret)) return false;
        if (string.IsNullOrWhiteSpace(timestamp)) return false;
        if (string.IsNullOrWhiteSpace(slackSignature)) return false;

        if (!long.TryParse(timestamp, out var ts)) return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var diff = Math.Abs(now - ts);
        if (diff > (long)tolerance.TotalSeconds) return false;

        var body = Encoding.UTF8.GetString(rawBody);
        var baseString = $"v0:{timestamp}:{body}";

        var keyBytes = Encoding.UTF8.GetBytes(signingSecret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        var computed = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();

        return FixedTimeEquals(computed, slackSignature);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
