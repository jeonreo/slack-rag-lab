namespace SlackRag.Domain.Rag;

public interface IAnswerGenerator
{
    Task<string> GenerateAsync(string question, string context, CancellationToken ct);
}
