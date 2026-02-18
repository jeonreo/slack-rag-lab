using MediatR;
using Microsoft.Extensions.Logging;

namespace SlackRag.Application.Common.Behaviors;

/// <summary>
/// 핸들러 예외를 공통 로깅한 뒤 상위로 전파한다.
/// </summary>
public sealed class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            // 요청 타입 기준으로 예외를 구조화 로그에 남긴다.
            var name = typeof(TRequest).Name;
            _logger.LogError(ex, "Unhandled exception for {RequestName}", name);
            throw;
        }
    }
}
