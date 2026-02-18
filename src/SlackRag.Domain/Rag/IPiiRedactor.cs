namespace SlackRag.Domain.Rag;

public interface IPiiRedactor
{
    string Redact(string input);
}
