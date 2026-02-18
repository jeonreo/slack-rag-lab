using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SlackRag.Application.Rag.Reindex;
using SlackRag.Application.Slack.Ingest;
using SlackRag.Application.Slack.Approval;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;
using SlackRag.Infrastructure.Security;
using SlackRag.Infrastructure.Slack;
using SlackRag.Infrastructure.Rag;
using System.Net.Http.Headers;
using SlackRag.Api.Batch;
using SlackRag.Api.Slack;
using System.Text;
using SlackRag.Application.Common.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// API 서버 설정
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RagOptions>(builder.Configuration.GetSection("Rag"));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SlackRag.Application.Rag.AskHandler).Assembly);
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

builder.Services.AddScoped<SlackRag.Domain.Rag.IKnowledgeCardSearch>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var connStr = cfg.GetConnectionString("RagDb");
    return new SlackRag.Infrastructure.Rag.PgKnowledgeCardSearch(connStr!);
});

builder.Services.AddScoped<IKnowledgeCardRepository>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new PgKnowledgeCardRepository(cfg.GetConnectionString("RagDb")!);
});

builder.Services.AddScoped<IPiiRedactor, PiiRedactorAdapter>();

builder.Services.AddScoped<SlackRag.Domain.Rag.IEmbeddingService, SlackRag.Infrastructure.OpenAi.OpenAiEmbeddingService>();
builder.Services.AddScoped<SlackRag.Domain.Rag.IAnswerGenerator, SlackRag.Infrastructure.OpenAi.OpenAiAnswerGenerator>();

builder.Services.AddHttpClient<ISlackClient, SlackWebApiClient>(client =>
{
    var token = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN");
    if (string.IsNullOrWhiteSpace(token))
        throw new InvalidOperationException("Missing SLACK_BOT_TOKEN");

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});


// 실행 모드 분기:
// - 기본: API 서버 실행
// - batch 인자: 배치 작업 실행 후 종료
if (args.Length > 0 && args[0].Equals("batch", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2 || !args[1].Equals("ingest", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Expected: batch ingest --channel <id> --windowHours 24 --dryRun true");
        Environment.Exit(2);
    }

    var parsed = BatchArgsParser.Parse(args);

    var appForBatch = builder.Build();

    using var scope = appForBatch.Services.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<SlackRag.Domain.Rag.IKnowledgeCardRepository>();
    await repo.EnsureIndexesAsync(CancellationToken.None);
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();



    var result = await mediator.Send(new IngestSlackHistoryCommand(
    ChannelId: parsed.Channel,
    WindowHours: parsed.WindowHours,
    PageSize: 200,
    DryRun: parsed.DryRun
));

    Console.WriteLine($"Batch done inserted={result.Inserted}");
    Environment.Exit(0);
}


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 운영에서는 리버스 프록시(Nginx/ALB) 구성을 함께 고려
app.UseHttpsRedirection();


// 1) 질문 응답 API

app.MapPost("/ask", async ([FromBody] AskRequest req, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new SlackRag.Application.Rag.AskQuery(req.Question), ct);
    return Results.Ok(new { result.Question, result.Answer, Hits = result.Hits });
});


// 2) 환경 변수 키 로드 확인용(디버깅)
app.MapGet("/checkkey", () =>
{
    var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    return string.IsNullOrEmpty(key) ? "No Key" : "Key Loaded";
})
.WithName("CheckKey");

// 3) 임베딩 재색인(관리용)
app.MapPost("/admin/reindex", async (IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new ReindexKnowledgeCardsCommand(), ct);
    return Results.Ok(new { Updated = result.Updated });
})
.WithName("Reindex");


// 4) Slack Events 수신
var approvedReactions = new HashSet<string>
{
    "blahblah_check",
    "blah_done",
    "white_check_mark",
    "heavy_check_mark"
};

app.MapPost("/slack/events", async (HttpRequest req, IMediator mediator, IConfiguration cfg, ILogger<Program> logger, CancellationToken ct) =>
{
    var timestamp = req.Headers["X-Slack-Request-Timestamp"].ToString();
    var signature = req.Headers["X-Slack-Signature"].ToString();
 
    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms, ct);
    var rawBody = ms.ToArray();

    var signingSecret =
        Environment.GetEnvironmentVariable("SLACK_SIGNING_SECRET")
        ?? cfg["Slack:SigningSecret"]
        ?? "";

    var ok = SlackRequestVerifier.Verify(
        signingSecret,
        timestamp,
        rawBody,
        signature,
        TimeSpan.FromMinutes(5)
    );

    if (!ok) return Results.Unauthorized();

    var body = Encoding.UTF8.GetString(rawBody);

    using var doc = JsonDocument.Parse(body);
    
    var root = doc.RootElement;

    if (root.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "url_verification")
    {
        var challenge = root.GetProperty("challenge").GetString();
        return Results.Json(new { challenge });
    }

    if (!root.TryGetProperty("event", out var ev)) return Results.Ok();

    var evType = ev.TryGetProperty("type", out var evTypeEl) ? evTypeEl.GetString() : null;
    if (evType != "reaction_added") return Results.Ok();

    var reaction = ev.TryGetProperty("reaction", out var rEl) ? rEl.GetString() : null;
    if (string.IsNullOrWhiteSpace(reaction)) return Results.Ok();

    var approved = cfg.GetSection("SlackApproval:ApprovedReactions").Get<string[]>() ?? Array.Empty<string>();
    if (!approved.Contains(reaction)) return Results.Ok();

    var item = ev.GetProperty("item");
    var itemType = item.GetProperty("type").GetString();
    if (itemType != "message") return Results.Ok();

    var channel = item.GetProperty("channel").GetString();
    var ts = item.GetProperty("ts").GetString();
    if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(ts)) return Results.Ok();

    // MediatR 호출. insert만 수행
    try
    {
        _ = await mediator.Send(new ApproveSlackMessageCommand(channel, ts, reaction), ct);    
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Slack event handler failed");
    }

    return Results.Ok();
});

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<SlackRag.Domain.Rag.IKnowledgeCardRepository>();
    await repo.EnsureIndexesAsync(CancellationToken.None);
}

app.Run();


// =========================================
// 공용 DTO
// =========================================
record AskRequest(string Question);
