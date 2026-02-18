namespace SlackRag.Api.Batch;

/// <summary>
/// 배치 수집 실행 인자 모델이다.
/// </summary>
public sealed record BatchArgs(string Channel, int WindowHours, bool DryRun);
