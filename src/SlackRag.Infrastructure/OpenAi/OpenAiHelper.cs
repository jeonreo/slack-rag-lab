using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SlackRag.Infrastructure.Security;


namespace SlackRag.Infrastructure.OpenAi;

/// <summary>
/// OpenAI HTTP 호출과 pgvector 포맷 변환을 담당하는 공용 헬퍼다.
/// </summary>
public static class OpenAiHelper
{
    /// <summary>
    /// 입력 텍스트 임베딩을 생성한다.
    /// </summary>
    public static async Task<float[]> CreateEmbeddingAsync(string input)
    {
        // API 호출 전 PII를 마스킹한다.
        input = PiiRedactor.Redact(input);

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not set.");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = "text-embedding-3-small",
            input = input
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await http.PostAsync("https://api.openai.com/v1/embeddings", content);
        var body = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var emb = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

        var result = new float[emb.GetArrayLength()];
        for (int i = 0; i < result.Length; i++)
            result[i] = emb[i].GetSingle();

        return result;
    }

    /// <summary>
    /// 질문+컨텍스트를 바탕으로 답변 텍스트를 생성한다.
    /// </summary>
    public static async Task<string> GenerateAnswerAsync(string question, string context)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not set.");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content =
                    "You are an internal assistant. Use only the provided context. " +
                    "If context is insufficient, do not guess. Ask for missing details. " +
                    "Never output personal/customer data; redact if present. " +
                    "Always output in this exact format:\n" +
                    "Answer: <short answer based only on context>\n" +
                    "Evidence: <card ids or key lines from context>\n" +
                    "NeedMoreInfo: <Yes or No>\n" +
                    "FollowUpQuestion: <one specific question if NeedMoreInfo is Yes, otherwise N/A>" },
                new { role = "user", content = $"Context:\n{context}\n\nQuestion:\n{question}" }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var body = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    /// <summary>
    /// float 벡터를 pgvector SQL literal 형식으로 변환한다.
    /// </summary>
    public static string ToPgVectorLiteral(float[] vec)
        => "[" + string.Join(",", vec.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
}
