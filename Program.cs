using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SlackRagBot;

var builder = WebApplication.CreateBuilder(args);

// 실행 모드 분기:
// - 기본: API 서버 실행
// - batch 인자: 배치 작업 실행 후 종료
if (args.Length > 0 && args[0].Equals("batch", StringComparison.OrdinalIgnoreCase))
{
    var exitCode = await RunBatchAsync(args, builder.Configuration);
    Environment.Exit(exitCode);
}

// API 서버 설정
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 운영에서는 리버스 프록시(Nginx/ALB) 구성을 함께 고려
app.UseHttpsRedirection();

// 1) 질문 응답 API
app.MapPost("/ask", async ([FromBody] AskRequest req, IConfiguration cfg) =>
{
    // 1. 질문 PII 마스킹 후 임베딩 생성
    var redactedQuestion = PiiRedactor.Redact(req.Question);
    var qVec = await OpenAiHelper.CreateEmbeddingAsync(redactedQuestion);
    var qLiteral = OpenAiHelper.ToPgVectorLiteral(qVec);

    // 2. 벡터 유사도 Top-K 검색
    await using var conn = new NpgsqlConnection(cfg.GetConnectionString("RagDb"));
    await conn.OpenAsync();

    await using var cmd = new NpgsqlCommand(@"
        SELECT id, problem, solution, source_url,
               (embedding <-> (@q::vector)) AS distance
        FROM knowledge_cards
        WHERE embedding IS NOT NULL
        ORDER BY embedding <-> (@q::vector)
        LIMIT 3;", conn);

    cmd.Parameters.AddWithValue("q", qLiteral);

    var hits = new List<object>();
    var contextChunks = new List<string>();

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var problem = reader.GetString(1);
        var solution = reader.GetString(2);
        var sourceUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
        var distance = reader.GetDouble(4);

        // 디버깅/가시성용 전체 hit 목록
        hits.Add(new
        {
            Id = id,
            Problem = problem,
            Solution = solution,
            SourceUrl = sourceUrl,
            Distance = distance
        });

        // LLM 컨텍스트는 거리 임계값 이내만 포함
        if (distance <= 1.0)
        {
            contextChunks.Add(
                $"[Card {id}]\nProblem: {problem}\nSolution: {solution}\nSource: {sourceUrl}\nDistance: {distance:0.####}"
            );
        }
    }

    // 3. 검색 결과 자체가 없으면 fallback
    if (hits.Count == 0)
    {
        return Results.Ok(new
        {
            req.Question,
            Answer = "I couldn't find relevant internal context. Can you share more details (service name / environment / error message)?",
            Hits = hits
        });
    }

    // 4. 검색 hit는 있어도 유효 컨텍스트가 없으면 추측 답변 금지
    if (contextChunks.Count == 0)
    {
        return Results.Ok(new
        {
            req.Question,
            Answer = "I found matches, but none are reliable enough to answer safely. Can you provide service name, environment, and exact error text?",
            Hits = hits
        });
    }

    // 5. top1 유사도가 약하면 추가질문 유도
    var top1Distance = (double)((dynamic)hits[0]).Distance;
    if (top1Distance > 0.90)
    {
        return Results.Ok(new
        {
            req.Question,
            Answer = "I found only weakly related context. Which service/component are you working on, and what environment (staging/prod) specifically?",
            Hits = hits
        });
    }

    // 6. 답변 생성 후 출력 전 PII 안전 필터
    var context = string.Join("\n\n", contextChunks);
    var answer = await OpenAiHelper.GenerateAnswerAsync(req.Question, context);
    var redactedAnswer = PiiRedactor.Redact(answer);

    var safeAnswer = PiiRedactor.LooksLikePii(redactedAnswer)
        ? "I can't provide that response safely. Please remove personal or sensitive data and try again."
        : redactedAnswer;

    return Results.Ok(new
    {
        req.Question,
        Answer = safeAnswer,
        Hits = hits
    });
})
.WithName("Ask");

// 2) 환경 변수 키 로드 확인용(디버깅)
app.MapGet("/checkkey", () =>
{
    var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    return string.IsNullOrEmpty(key) ? "No Key" : "Key Loaded";
})
.WithName("CheckKey")
.WithOpenApi();

// 3) 임베딩 재색인(관리용)
app.MapPost("/admin/reindex", async (IConfiguration cfg) =>
{
    await using var conn = new NpgsqlConnection(cfg.GetConnectionString("RagDb"));
    await conn.OpenAsync();

    var cards = new List<(int Id, string Problem, string Solution)>();

    await using (var cmd = new NpgsqlCommand(@"
        SELECT id, problem, solution
        FROM knowledge_cards
        WHERE embedding IS NULL;", conn))
    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
            cards.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
    }

    foreach (var c in cards)
    {
        // DB 저장 전/임베딩 전 모두 마스킹 적용
        var redactedProblem = PiiRedactor.Redact(c.Problem);
        var redactedSolution = PiiRedactor.Redact(c.Solution);
        var redactedText = $"Problem: {redactedProblem}\nSolution: {redactedSolution}";

        var vec = await OpenAiHelper.CreateEmbeddingAsync(redactedText);
        var vecLiteral = OpenAiHelper.ToPgVectorLiteral(vec);

        await using var upd = new NpgsqlCommand(@"
            UPDATE knowledge_cards
            SET problem = @problem,
                solution = @solution,
                embedding = @embedding::vector
            WHERE id = @id;", conn);

        upd.Parameters.AddWithValue("id", c.Id);
        upd.Parameters.AddWithValue("problem", redactedProblem);
        upd.Parameters.AddWithValue("solution", redactedSolution);
        upd.Parameters.AddWithValue("embedding", vecLiteral);

        await upd.ExecuteNonQueryAsync();
    }

    return Results.Ok(new { Updated = cards.Count });
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

app.MapPost("/slack/events", async (HttpRequest req) =>
{
    // 요청 body 파싱
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;

    // Slack URL verification
    if (root.TryGetProperty("type", out var type) && type.GetString() == "url_verification")
    {
        var challenge = root.GetProperty("challenge").GetString();
        return Results.Json(new { challenge });
    }

    // 이벤트 콜백 처리
    if (root.TryGetProperty("event", out var ev))
    {
        var evType = ev.GetProperty("type").GetString();

        if (evType == "reaction_added")
        {
            var reaction = ev.GetProperty("reaction").GetString();
            var item = ev.GetProperty("item");
            var itemType = item.GetProperty("type").GetString();

            // 승인 이모지 allowlist에 포함된 경우만 카드 후보로 간주
            if (itemType == "message" && reaction != null && approvedReactions.Contains(reaction))
            {
                var channel = item.GetProperty("channel").GetString();
                var ts = item.GetProperty("ts").GetString();

                Console.WriteLine($"ApprovedMessage channel={channel} ts={ts} reaction={reaction}");
            }
            else
            {
                Console.WriteLine($"IgnoredReaction reaction={reaction}");
            }
        }
    }

    return Results.Ok();
});

app.Run();

// Batch 실행 로직
static async Task<int> RunBatchAsync(string[] args, IConfiguration cfg)
{
    try
    {
        var cmd = args.Length > 1 ? args[1] : string.Empty;
        if (!cmd.Equals("ingest", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Expected: batch ingest");
            return 2;
        }

        var parsed = ParseBatchArgs(args);

        var slackToken = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(slackToken))
        {
            Console.WriteLine("Missing SLACK_BOT_TOKEN");
            return 2;
        }

        var connStr = cfg.GetConnectionString("RagDb");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.WriteLine("Missing connection string: RagDb");
            return 2;
        }

        Console.WriteLine($"Batch start channel={parsed.Channel} windowHours={parsed.WindowHours}");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", slackToken);

        // v0: 최근 메시지 20개만 가져오기
        var url = $"https://slack.com/api/conversations.history?channel={parsed.Channel}&limit=20";
        var json = await http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.GetProperty("ok").GetBoolean())
        {
            var err = doc.RootElement.TryGetProperty("error", out var errEl)
                ? errEl.GetString()
                : "unknown_error";
            Console.WriteLine($"Slack API failed: {err}");
            return 3;
        }

        var messages = doc.RootElement.GetProperty("messages");

        await using var conn = new Npgsql.NpgsqlConnection(connStr);
        await conn.OpenAsync();

        int inserted = 0;

        foreach (var m in messages.EnumerateArray())
        {
            var text = m.TryGetProperty("text", out var tEl) ? (tEl.GetString() ?? "") : "";
            if (string.IsNullOrWhiteSpace(text)) continue;

            var ts = m.TryGetProperty("ts", out var tsEl) ? tsEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(ts)) continue;

            var sourceUrl = $"slack://{parsed.Channel}/{ts}";

            await using var ins = new Npgsql.NpgsqlCommand(@"
                INSERT INTO knowledge_cards(problem, solution, source_url, embedding)
                VALUES (@problem, @solution, @source_url, NULL);", conn);

            ins.Parameters.AddWithValue("problem", PiiRedactor.Redact(text.Trim()));
            ins.Parameters.AddWithValue("solution", "TBD");
            ins.Parameters.AddWithValue("source_url", sourceUrl);

            await ins.ExecuteNonQueryAsync();
            inserted++;
        }

        Console.WriteLine($"Batch done inserted={inserted}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Batch failed: " + ex);
        return 5;
    }
}

static BatchArgs ParseBatchArgs(string[] args)
{
    // 기본값
    string? channel = null;
    int windowHours = 24;
    bool dryRun = false;

    // args 예시
    // [0]=batch [1]=ingest --channel C0... --windowHours 24 --dryRun true
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--channel" && i + 1 < args.Length) channel = args[i + 1];
        if (args[i] == "--windowHours" && i + 1 < args.Length && int.TryParse(args[i + 1], out var w)) windowHours = w;
        if (args[i] == "--dryRun" && i + 1 < args.Length && bool.TryParse(args[i + 1], out var d)) dryRun = d;
    }

    if (string.IsNullOrWhiteSpace(channel))
        throw new ArgumentException("Missing required --channel");

    return new BatchArgs(channel, windowHours, dryRun);
}

record BatchArgs(string Channel, int WindowHours, bool DryRun);

// =========================================
// 공용 DTO
// =========================================
record AskRequest(string Question);
