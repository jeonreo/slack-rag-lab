using Npgsql;
using SlackRag.Domain.Rag;
using SlackRag.Infrastructure.OpenAi;

namespace SlackRag.Infrastructure.Rag;

public sealed class PgKnowledgeCardRepository : IKnowledgeCardRepository
{
    private readonly string _connStr;

    public PgKnowledgeCardRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<IReadOnlyList<KnowledgeCardForIndexing>> GetCardsMissingEmbeddingAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
            SELECT id, problem, solution
            FROM knowledge_cards
            WHERE embedding IS NULL;", conn);

        var list = new List<KnowledgeCardForIndexing>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new KnowledgeCardForIndexing(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return list;
    }

    public async Task UpdateCardEmbeddingAsync(
        int id,
        string problem,
        string solution,
        float[] embedding,
        CancellationToken ct
    )
    {
        var vecLiteral = OpenAiHelper.ToPgVectorLiteral(embedding);

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
            UPDATE knowledge_cards
            SET problem = @problem,
                solution = @solution,
                embedding = @embedding::vector
            WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("problem", problem);
        cmd.Parameters.AddWithValue("solution", solution);
        cmd.Parameters.AddWithValue("embedding", vecLiteral);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> InsertKnowledgeCardAsync(
    string problem,
    string solution,
    string sourceUrl,
    CancellationToken ct
)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
    INSERT INTO knowledge_cards(problem, solution, source_url, embedding)
    VALUES (@problem, @solution, @source_url, NULL)
    ON CONFLICT (source_url) DO NOTHING;", conn);

        cmd.Parameters.AddWithValue("problem", problem);
        cmd.Parameters.AddWithValue("solution", solution);
        cmd.Parameters.AddWithValue("source_url", sourceUrl);

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
    CREATE UNIQUE INDEX IF NOT EXISTS ux_knowledge_cards_source_url
ON public.knowledge_cards(source_url);
", conn);


        await cmd.ExecuteNonQueryAsync(ct);
    }
}

