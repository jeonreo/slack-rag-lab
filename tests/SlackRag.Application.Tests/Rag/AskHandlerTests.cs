using Moq;
using SlackRag.Application.Rag;
using SlackRag.Domain.Rag;
using Microsoft.Extensions.Options;
using Xunit;



namespace SlackRag.Application.Tests.Rag;

public class AskHandlerTests
{
    [Fact]
    public async Task No_hits_returns_fallback()
    {
        var embed = new Mock<IEmbeddingService>();
        var search = new Mock<IKnowledgeCardSearch>();
        var ans = new Mock<IAnswerGenerator>();

        embed.Setup(x => x.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        search.Setup(x => x.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeCardHit>());

        var opt = Options.Create(new RagOptions
        {
            TopK = 3,
            MaxDistance = 1.0,
            WeakThreshold = 0.90
        });

        var handler = new AskHandler(embed.Object, search.Object, ans.Object, opt);

        var result = await handler.Handle(new AskQuery("q"), default);

        Assert.Contains("couldn't find", result.Answer);
    }

    [Fact]
    public async Task Hits_but_all_filtered_out_returns_safe_message()
    {
        var embed = new Mock<IEmbeddingService>();
        var search = new Mock<IKnowledgeCardSearch>();
        var ans = new Mock<IAnswerGenerator>();

        embed.Setup(x => x.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        search.Setup(x => x.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new KnowledgeCardHit(1, "p", "s", null, 1.5) // distance > 1.0 이라 context 제외
            });

        var opt = Options.Create(new RagOptions
        {
            TopK = 3,
            MaxDistance = 1.0,
            WeakThreshold = 0.90
        });

        var handler = new AskHandler(embed.Object, search.Object, ans.Object, opt);

        var result = await handler.Handle(new AskQuery("q"), default);

        Assert.Contains("reliable enough", result.Answer);
    }
}
