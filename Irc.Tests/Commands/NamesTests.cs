using Irc.Commands;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Moq;

namespace Irc.Tests.Commands;

[TestFixture]
public class NamesTests
{
    [Test]
    public void Execute_NoParameters_SendsNamesAndSingleEndOfNamesMarker()
    {
        var sentMessages = new List<string>();
        var mockServer = new Mock<IServer>();
        var mockUser = new Mock<IUser>();
        var mockProtocol = new Mock<IProtocol>();
        var channel = new Channel("%#chat");

        mockServer.Setup(s => s.ToString()).Returns("MockServer");
        mockServer.Setup(s => s.GetChannels()).Returns(new List<IChannel> { channel });

        mockProtocol.Setup(p => p.FormattedUser(It.IsAny<IChannelMember>())).Returns("TestUser");
        mockUser.Setup(u => u.Server).Returns(mockServer.Object);
        mockUser.Setup(u => u.ToString()).Returns("TestUser");
        mockUser.Setup(u => u.IsOn(It.IsAny<IChannel>())).Returns(false);
        mockUser.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);
        mockUser.Setup(u => u.Send(It.IsAny<string>())).Callback((string message) => sentMessages.Add(message));

        var mockChatMessage = new Mock<IChatMessage>();
        mockChatMessage.Setup(m => m.Parameters).Returns(new List<string>());

        var mockChatFrame = new Mock<IChatFrame>();
        mockChatFrame.Setup(f => f.Server).Returns(mockServer.Object);
        mockChatFrame.Setup(f => f.User).Returns(mockUser.Object);
        mockChatFrame.Setup(f => f.ChatMessage).Returns(mockChatMessage.Object);

        ExecuteNames(mockChatFrame.Object);

        Assert.That(sentMessages.Count(m => m.Contains(" 366 ")), Is.EqualTo(1));
        Assert.That(sentMessages.Any(m => m.Contains(" 366 ") && m.Contains(" * ")), Is.True);
        Assert.That(sentMessages.Any(m => m.Contains(" 353 ")), Is.True);
    }

    [Test]
    public void Execute_UnknownChannel_SendsEndOfNamesOnly()
    {
        var sentMessages = new List<string>();
        var mockServer = new Mock<IServer>();
        var mockUser = new Mock<IUser>();
        var mockChatMessage = new Mock<IChatMessage>();
        var mockChatFrame = new Mock<IChatFrame>();

        mockServer.Setup(s => s.ToString()).Returns("MockServer");
        mockServer.Setup(s => s.GetChannelByName(It.IsAny<string>())).Returns((IChannel?)null);
        mockUser.Setup(u => u.ToString()).Returns("TestUser");
        mockUser.Setup(u => u.Send(It.IsAny<string>())).Callback((string message) => sentMessages.Add(message));
        mockChatMessage.Setup(m => m.Parameters).Returns(new List<string> { "%#missing" });
        mockChatFrame.Setup(f => f.Server).Returns(mockServer.Object);
        mockChatFrame.Setup(f => f.User).Returns(mockUser.Object);
        mockChatFrame.Setup(f => f.ChatMessage).Returns(mockChatMessage.Object);

        ExecuteNames(mockChatFrame.Object);

        Assert.That(sentMessages.Any(m => m.Contains(" 366 ") && m.Contains(" %#missing ")), Is.True);
        Assert.That(sentMessages.Any(m => m.Contains(" 403 ")), Is.False);
    }

    private static void ExecuteNames(IChatFrame chatFrame)
    {
#pragma warning disable IL2026, IL2075, IL2072
        var namesType = typeof(Join).Assembly.GetType("Irc.Commands.Names", throwOnError: true)!;
        var execute = namesType.GetMethod("Execute");
        var command = Activator.CreateInstance(namesType)!;
        execute!.Invoke(command, new object[] { chatFrame });
#pragma warning restore IL2026, IL2075, IL2072
    }
}
