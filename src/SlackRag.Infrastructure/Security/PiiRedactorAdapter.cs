using SlackRag.Domain.Rag;

namespace SlackRag.Infrastructure.Security;

public sealed class PiiRedactorAdapter : IPiiRedactor
{
    public string Redact(string input) => PiiRedactor.Redact(input);
}
