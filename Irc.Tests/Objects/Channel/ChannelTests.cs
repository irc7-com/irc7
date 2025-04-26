using Irc.Interfaces;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Objects.Channel;

[TestFixture]
public class ChannelTests
{
    [Test]
    public void GetMemberByNickname_ShouldBeCaseInsensitive()
    {
        // Arrange
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtectionProfile = new Mock<IFloodProtectionProfile>();
        var mockServer = new Mock<IServer>();

        mockConnection.Setup(x => x.GetIp()).Returns("127.0.0.1");

        var channel = new Irc.Objects.Channel.Channel("TestChannel");
        var user1 = new User(mockConnection.Object, mockProtocol.Object, mockDataRegulator.Object, mockFloodProtectionProfile.Object, mockServer.Object)
        {
            Nickname = "TestUser",
        };
        var user2 = new User(mockConnection.Object, mockProtocol.Object, mockDataRegulator.Object, mockFloodProtectionProfile.Object, mockServer.Object)
        {
            Nickname = "AnotherUser",
        };

        channel.Join(user1);
        channel.Join(user2);

        // Act
        var result = channel.GetMemberByNickname("testuser");

        // Assert
        Assert.That(result, Is.Not.Null, "Member should be found regardless of case.");
        Assert.That(result.GetUser(), Is.EqualTo(user1), "The correct user should be returned.");
    }
}