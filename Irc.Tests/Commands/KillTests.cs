using Irc.Commands;
using Irc.Interfaces;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

[TestFixture]
public class KillTests
{
    private Mock<IServer> _mockServer = null!;
    private Mock<IUser> _mockAdmin = null!;
    private Mock<IChatFrame> _mockChatFrame = null!;
    private Mock<IChatMessage> _mockChatMessage = null!;
    private Dictionary<IChannel, IChannelMember> _adminChannels = null!;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockAdmin = new Mock<IUser>();
        _mockChatFrame = new Mock<IChatFrame>();
        _mockChatMessage = new Mock<IChatMessage>();
        _adminChannels = new Dictionary<IChannel, IChannelMember>();

        var adminAddress = new UserAddress
        {
            RemoteIp = "127.0.0.1",
            User = "admin",
            Host = "localhost"
        };
        adminAddress.SetNickname("Administrator");

        _mockAdmin.Setup(u => u.GetLevel()).Returns(Enumerations.EnumUserAccessLevel.Sysop);
        _mockAdmin.Setup(u => u.GetAddress()).Returns(adminAddress);
        _mockAdmin.Setup(u => u.GetChannels()).Returns(_adminChannels);
        _mockAdmin.Setup(u => u.ToString()).Returns("Administrator");

        _mockServer.Setup(s => s.ToString()).Returns("MockServer");

        _mockChatMessage.Setup(m => m.Parameters).Returns(new List<string> { "DupNick", "Test reason" });

        _mockChatFrame.Setup(f => f.Server).Returns(_mockServer.Object);
        _mockChatFrame.Setup(f => f.User).Returns(_mockAdmin.Object);
        _mockChatFrame.Setup(f => f.ChatMessage).Returns(_mockChatMessage.Object);
    }

    [Test]
    public void Execute_TargetNickNotInAdminChannel_SendsNoSuchNick()
    {
        var sourceChannel = new Mock<IChannel>();
        var sourceMember = new Mock<IChannelMember>();
        var otherUser = CreateMockUser("OtherNick").Object;

        sourceMember.Setup(m => m.GetUser()).Returns(otherUser);
        sourceChannel.Setup(c => c.GetMembers()).Returns(new List<IChannelMember> { sourceMember.Object });
        _adminChannels.Add(sourceChannel.Object, sourceMember.Object);

        _mockServer.Setup(s => s.GetUsers()).Returns(new List<IUser> { CreateMockUser("DupNick").Object });

        var kill = new Kill();
        kill.Execute(_mockChatFrame.Object);

        _mockAdmin.Verify(u => u.Send(It.Is<string>(raw => raw.Contains(" 401 "))), Times.Once);
    }

    [Test]
    public void Execute_AdminNotInChannel_SendsUserNotInChannel()
    {
        _mockServer.Setup(s => s.GetUsers()).Returns(new List<IUser> { CreateMockUser("DupNick").Object });

        var kill = new Kill();
        kill.Execute(_mockChatFrame.Object);

        _mockAdmin.Verify(u => u.Send(It.Is<string>(raw => raw.Contains(" 928 "))), Times.Once);
    }

    [Test]
    public void Execute_TargetNickInAdminChannel_KillsAllMatchingNicknameConnections()
    {
        var sourceChannel = new Mock<IChannel>();
        var sourceMember = new Mock<IChannelMember>();
        var sourceTarget = CreateMockUser("DupNick");

        sourceMember.Setup(m => m.GetUser()).Returns(sourceTarget.Object);
        sourceChannel.Setup(c => c.GetMembers()).Returns(new List<IChannelMember> { sourceMember.Object });
        _adminChannels.Add(sourceChannel.Object, sourceMember.Object);

        var duplicateTarget = CreateMockUser("DupNick");
        var otherUser = CreateMockUser("OtherNick");

        _mockServer.Setup(s => s.GetUsers()).Returns(new List<IUser>
        {
            sourceTarget.Object,
            duplicateTarget.Object,
            otherUser.Object
        });

        var disconnectCount = 0;
        sourceTarget.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);
        duplicateTarget.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);
        otherUser.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);

        var kill = new Kill();
        kill.Execute(_mockChatFrame.Object);

        Assert.That(disconnectCount, Is.EqualTo(2), "All matching users should be disconnected.");
        otherUser.Verify(u => u.Disconnect(It.IsAny<string>()), Times.Never);
    }

    private static Mock<IUser> CreateMockUser(string nickname)
    {
        var user = new Mock<IUser>();
        var channels = new Dictionary<IChannel, IChannelMember>();

        var address = new UserAddress
        {
            RemoteIp = "127.0.0.1",
            User = "user",
            Host = "localhost"
        };
        address.SetNickname(nickname);

        user.Setup(u => u.GetAddress()).Returns(address);
        user.Setup(u => u.GetLevel()).Returns(Enumerations.EnumUserAccessLevel.None);
        user.Setup(u => u.GetChannels()).Returns(channels);
        user.Setup(u => u.RemoveChannel(It.IsAny<IChannel>()))
            .Callback((IChannel channel) => channels.Remove(channel));
        user.Setup(u => u.ToString()).Returns(nickname);

        var targetChannel = new Mock<IChannel>();
        var targetMember = new Mock<IChannelMember>();
        targetMember.Setup(m => m.GetUser()).Returns(user.Object);
        var members = new List<IChannelMember> { targetMember.Object };
        targetChannel.Setup(c => c.GetMembers()).Returns(members);
        targetChannel.Setup(c => c.ToString()).Returns("%#test");

        channels[targetChannel.Object] = targetMember.Object;

        return user;
    }
}
