using Moq;
using SlackRag.Application.Slack.Ingest;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;
using Xunit;

namespace SlackRag.Application.Tests.Slack.Ingest;

public class IngestSlackHistoryHandlerTests
{
    [Fact]
    public async Task Dry_run_counts_without_inserting()
    {
        var slack = new Mock<ISlackClient>();
        var repo = new Mock<IKnowledgeCardRepository>();
        var pii = new Mock<IPiiRedactor>();

        slack.Setup(x => x.GetMessagesAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SlackMessage("1", "hello", DateTimeOffset.UtcNow),
                new SlackMessage("2", "world", DateTimeOffset.UtcNow)
            });

        pii.Setup(x => x.Redact(It.IsAny<string>())).Returns<string>(x => x);

        var handler = new IngestSlackHistoryHandler(slack.Object, repo.Object, pii.Object);

        var result = await handler.Handle(new IngestSlackHistoryCommand(
            ChannelId: "C1",
            WindowHours: 24,
            PageSize: 200,
            DryRun: true
        ), default);

        Assert.Equal(2, result.Inserted);

        repo.Verify(x => x.InsertKnowledgeCardAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
