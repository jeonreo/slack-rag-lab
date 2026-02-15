using System.Text.RegularExpressions;

namespace SlackRagBot;

public static class PiiRedactor
{
    // Goal: reduce accidental leakage risk in text passed to storage/model/output.
    // Regex-based masking is heuristic, so output-side validation is still required.

    private static readonly Regex Email = new(
        @"(?i)\b[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}\b",
        RegexOptions.Compiled);

    // International/local phone number candidates.
    private static readonly Regex Phone = new(
        @"\b(?:\+?\d{1,3}[-.\s]?)?(?:\(?\d{2,4}\)?[-.\s]?)?\d{3,4}[-.\s]?\d{4}\b",
        RegexOptions.Compiled);

    // API key/token candidates. Add more patterns for your org if needed.
    private static readonly Regex OpenAiKey = new(
        @"\bsk-[A-Za-z0-9]{20,}\b",
        RegexOptions.Compiled);

    private static readonly Regex AwsAccessKeyId = new(
        @"\bAKIA[0-9A-Z]{16}\b",
        RegexOptions.Compiled);

    // JWT-like pattern. May over-match depending on text characteristics.
    private static readonly Regex JwtLike = new(
        @"\beyJ[A-Za-z0-9_\-]+=*\.[A-Za-z0-9_\-]+=*\.[A-Za-z0-9_\-]+=*\b",
        RegexOptions.Compiled);

    // Credit card candidate pattern (disabled by default due to false positives).
    private static readonly Regex CreditCard = new(
        @"\b(?:\d[ -]*?){13,19}\b",
        RegexOptions.Compiled);

    // Korean resident registration number candidate.
    private static readonly Regex KrRrn = new(
        @"\b\d{6}-?\d{7}\b",
        RegexOptions.Compiled);

    // Add organization-specific IDs if needed (ex: ORD-123456, CUST_98765).

    public static string Redact(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input ?? string.Empty;

        var s = input;

        s = Email.Replace(s, "[REDACTED_EMAIL]");
        s = Phone.Replace(s, "[REDACTED_PHONE]");
        s = OpenAiKey.Replace(s, "[REDACTED_API_KEY]");
        s = AwsAccessKeyId.Replace(s, "[REDACTED_AWS_KEY]");
        s = JwtLike.Replace(s, "[REDACTED_TOKEN]");
        s = KrRrn.Replace(s, "[REDACTED_RRN]");

        // Disabled by default to avoid masking non-sensitive numeric data.
        // s = CreditCard.Replace(s, "[REDACTED_CARD]");

        return s;
    }

    // Secondary check before returning user-visible output.
    public static bool LooksLikePii(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        return Email.IsMatch(input)
            || Phone.IsMatch(input)
            || OpenAiKey.IsMatch(input)
            || AwsAccessKeyId.IsMatch(input)
            || JwtLike.IsMatch(input)
            || KrRrn.IsMatch(input);
    }
}
