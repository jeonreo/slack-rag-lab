using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SlackRag.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
    var name = typeof(TRequest).Name;
    var sw = Stopwatch.StartNew();

    _logger.LogInformation("MediatR start {RequestName}", name);

    var response = await next();

    sw.Stop();
    _logger.LogInformation("MediatR end {RequestName} elapsedMs={ElapsedMs}", name, sw.ElapsedMilliseconds);

    return response;
}

}
