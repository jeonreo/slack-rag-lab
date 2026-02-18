using Npgsql;
using SlackRag.Domain.Rag;
using SlackRag.Infrastructure.OpenAi;

namespace SlackRag.Infrastructure.Rag;

/// <summary>
/// PostgreSQL pgvector 유사도 검색 구현체다.
/// </summary>
public sealed class PgKnowledgeCardSearch : IKnowledgeCardSearch
{
    private readonly string _connStr;

    public PgKnowledgeCardSearch(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<IReadOnlyList<KnowledgeCardHit>> SearchAsync(
        float[] questionEmbedding,
        int limit,
        CancellationToken ct
    )
    {
        // 질의 벡터를 SQL에서 사용할 pgvector literal로 변환한다.
        var qLiteral = OpenAiHelper.ToPgVectorLiteral(questionEmbedding);

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
            SELECT id, problem, solution, source_url,
                   (embedding <-> (@q::vector)) AS distance
            FROM knowledge_cards
            WHERE embedding IS NOT NULL
            ORDER BY embedding <-> (@q::vector)
            LIMIT @limit;", conn);

        cmd.Parameters.AddWithValue("q", qLiteral);
        cmd.Parameters.AddWithValue("limit", limit);

        // distance 오름차순 결과를 도메인 hit 모델로 매핑한다.
        var hits = new List<KnowledgeCardHit>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            hits.Add(new KnowledgeCardHit(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetDouble(4)
            ));
        }

        return hits;
    }
}
