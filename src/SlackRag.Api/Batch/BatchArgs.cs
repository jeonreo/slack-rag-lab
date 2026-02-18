namespace SlackRag.Api.Batch;

public sealed record BatchArgs(string Channel, int WindowHours, bool DryRun);
