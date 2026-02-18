using Moq;
using SlackRag.Application.Rag.Reindex;
using SlackRag.Domain.Rag;
using Xunit;

namespace SlackRag.Application.Tests.Rag.Reindex;

public class ReindexKnowledgeCardsHandlerTests
{
    [Fact]
    public async Task No_cards_returns_zero()
    {
        var repo = new Mock<IKnowledgeCardRepository>();
        var embed = new Mock<IEmbeddingService>();
        var pii = new Mock<IPiiRedactor>();

        repo.Setup(x => x.GetCardsMissingEmbeddingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeCardForIndexing>());

        var handler = new ReindexKnowledgeCardsHandler(repo.Object, embed.Object, pii.Object);

        var result = await handler.Handle(new ReindexKnowledgeCardsCommand(), default);

        Assert.Equal(0, result.Updated);
    }
}
