using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;
using Moq;

namespace Irc.Tests.Objects;

[TestFixture]
public class PropRuleTests
{
    [Test]
    public void BroadcastAccessLevel_ShouldDefaultToReadAccessLevel()
    {
        var rule = new PropRule("TEST", EnumChannelAccessLevel.ChatHost, EnumChannelAccessLevel.ChatOwner, ".*", string.Empty);

        Assert.That(rule.BroadcastAccessLevel, Is.EqualTo(EnumChannelAccessLevel.ChatHost));
    }

    [Test]
    public void EvaluateGet_ShouldDenyWhenReadAccessLevelIsNone_ForChannelMembers()
    {
        var user = CreateUser();
        var channel = new Irc.Objects.Channel.Channel("TestChannel");
        channel.Join(user);

        var rule = new PropRule("SECRET", EnumChannelAccessLevel.None, EnumChannelAccessLevel.ChatOwner, ".*", string.Empty);

        var result = rule.EvaluateGet(user, channel);

        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS));
    }

    [Test]
    public void OwnerKey_ShouldBroadcastToMembers_ButRemainUnreadable()
    {
        var user = CreateUser();
        var channel = new Irc.Objects.Channel.Channel("TestChannel");
        channel.Join(user);

        var ownerKey = channel.Props.GetProp(Resources.ChannelPropOwnerkey);

        Assert.That(ownerKey, Is.Not.Null);
        Assert.That(ownerKey!.BroadcastAccessLevel, Is.EqualTo(EnumChannelAccessLevel.ChatMember));
        Assert.That(ownerKey.EvaluateGet(user, channel), Is.EqualTo(EnumIrcError.ERR_NOPERMS));
    }

    private static Irc.Objects.User.User CreateUser()
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtectionProfile = new Mock<IFloodProtectionProfile>();
        var mockServer = new Mock<IServer>();
        var mockSaslHandler = new Mock<ISaslHandler>();

        mockConnection.Setup(x => x.GetIp()).Returns("127.0.0.1");

        return new Irc.Objects.User.User(
            mockConnection.Object,
            mockProtocol.Object,
            mockDataRegulator.Object,
            mockFloodProtectionProfile.Object,
            mockServer.Object,
            _ => mockSaslHandler.Object)
        {
            Nickname = "TestUser"
        };
    }
}

