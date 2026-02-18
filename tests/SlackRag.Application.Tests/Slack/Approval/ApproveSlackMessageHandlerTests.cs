using Moq;
using SlackRag.Application.Slack.Approval;
using SlackRag.Domain.Rag;
using SlackRag.Domain.Slack;
using Xunit;

namespace SlackRag.Application.Tests.Slack.Approval;

public class ApproveSlackMessageHandlerTests
{
    [Fact]
    public async Task Duplicate_returns_false()
    {
        var slack = new Mock<ISlackClient>();
        var repo = new Mock<IKnowledgeCardRepository>();
        var pii = new Mock<IPiiRedactor>();

        slack.Setup(x => x.GetMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SlackMessage("1.1", "hello", DateTimeOffset.UtcNow));

        pii.Setup(x => x.Redact(It.IsAny<string>())).Returns<string>(x => x);

        repo.Setup(x => x.InsertKnowledgeCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new ApproveSlackMessageHandler(slack.Object, repo.Object, pii.Object);

        var result = await handler.Handle(new ApproveSlackMessageCommand("C1", "1.1", "white_check_mark"), default);

        Assert.False(result.Inserted);
    }
}
