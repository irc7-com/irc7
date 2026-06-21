using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;
using EventCommand = Irc.Commands.Event;
using UserObject = Irc.Objects.User.User;

namespace Irc.Tests.Commands;

[TestFixture]
public class OnStageTests
{
    [Test]
    public void FromInMemoryChannel_PreservesAuditoriumAndOnStageModes()
    {
        var inMemoryChannel = new InMemoryChannel
        {
            ChannelName = "%#Stage",
            Modes = "xg"
        };

        var channel = Channel.FromInMemoryChannel(inMemoryChannel);

        Assert.That(channel.Modes.Auditorium.ModeValue, Is.True);
        Assert.That(channel.Modes.OnStage.ModeValue, Is.True);
    }

    [Test]
    public void JoinCloneableFullOnStageChannel_CreatesCloneWithAuditoriumAndOnStageModes()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var parent = new Channel("%#Stage");
        parent.Modes.Cloneable.ModeValue = true;
        parent.Modes.UserLimit.Value = 1;
        parent.Modes.Auditorium.ModeValue = true;
        parent.Modes.OnStage.ModeValue = true;
        channels.Add(parent);

        var hostMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        parent.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);

        var guestMessages = new List<string>();
        var guest = CreateUser("Guest", server.Object, guestMessages);
        Clear(hostMessages, guestMessages);

        Join.JoinChannels(server.Object, guest.Object, new List<string> { "%#Stage" }, string.Empty);

        var clone = channels.FirstOrDefault(c =>
            string.Equals(c.GetName(), "%#Stage1", StringComparison.OrdinalIgnoreCase));
        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.Modes.Auditorium.ModeValue, Is.True);
        Assert.That(clone.Modes.OnStage.ModeValue, Is.True);
        Assert.That(clone.Modes.Clone.ModeValue, Is.True);
    }

    [Test]
    public void Names_HidesOtherSpectatorsFromAuditoriumSpectator()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var aliceMessages = new List<string>();
        var bobMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var alice = CreateUser("Alice", server.Object, aliceMessages);
        var bob = CreateUser("Bob", server.Object, bobMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(alice.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        channel.Join(bob.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, aliceMessages, bobMessages);

        channel.SendNames(alice.Object);

        var namesReply = aliceMessages.Single(m => m.Contains(" 353 "));
        Assert.That(namesReply, Does.Contain("Alice"));
        Assert.That(namesReply, Does.Contain("Host"));
        Assert.That(namesReply, Does.Not.Contain("Bob"));
    }

    [Test]
    public void Names_ShowsFullAuditoriumAudienceToHosts()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var aliceMessages = new List<string>();
        var bobMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var alice = CreateUser("Alice", server.Object, aliceMessages);
        var bob = CreateUser("Bob", server.Object, bobMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(alice.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        channel.Join(bob.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, aliceMessages, bobMessages);

        channel.SendNames(host.Object);

        var namesReply = hostMessages.Single(m => m.Contains(" 353 "));
        Assert.That(namesReply, Does.Contain("Alice"));
        Assert.That(namesReply, Does.Contain("Bob"));
        Assert.That(namesReply, Does.Contain("Host"));
    }

    [Test]
    public void Privmsg_BlocksAuditoriumSpectatorSpeech()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var speakerMessages = new List<string>();
        var speaker = CreateRealUser("Alice", server.Object, speakerMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(speaker, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, speakerMessages);

        Privmsg.SendMessage(CreateChatFrame(server.Object, speaker, "%#Stage", "Hello"), false);

        Assert.That(speakerMessages.Any(m => m.Contains(" 404 ")), Is.True);
        Assert.That(hostMessages.Any(m => m.Contains(" PRIVMSG %#Stage ")), Is.False);
    }

    [Test]
    public void Privmsg_AllowsVoicedAuditoriumParticipantSpeech()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var speakerMessages = new List<string>();
        var speaker = CreateRealUser("Alice", server.Object, speakerMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(speaker, EnumChannelAccessResult.SUCCESS_VOICE);
        Clear(hostMessages, speakerMessages);

        Privmsg.SendMessage(CreateChatFrame(server.Object, speaker, "%#Stage", "Hello"), false);

        Assert.That(speakerMessages.Any(m => m.Contains(" 404 ")), Is.False);
        Assert.That(hostMessages.Any(m => m.Contains(" PRIVMSG %#Stage :Hello")), Is.True);
    }

    [Test]
    public void Esubmit_AddsQuestionToHostQueueWithoutPublicEquestion()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Esubmit().Execute(CreateChatFrame(server.Object, spectator.Object, "%#Stage", "Question?"));

        var question = channel.GetOnStageQuestions().Single();
        Assert.That(question.Nickname, Is.EqualTo("Alice"));
        Assert.That(question.SourceRoom, Is.EqualTo("%#Stage"));
        Assert.That(question.Message, Is.EqualTo("Question?"));
        Assert.That(hostMessages.Any(m => m.Contains(" 806 ") &&
                                          m.Contains("%#Stage 1 Alice %#Stage :Question?")), Is.True);
        Assert.That(hostMessages.Concat(spectatorMessages).Any(m => m.Contains(" EQUESTION ")), Is.False);
    }

    [Test]
    public void Esubmit_FromLinkedSourceRoomQueuesQuestionForStageHosts()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var stage = CreateOnStageChannel(channels);
        var source = new Channel("%#The\\bLobby");
        channels.Add(source);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        stage.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        source.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Esubmit().Execute(CreateChatFrame(server.Object, spectator.Object, "%#Stage", "Question?"));

        var question = stage.GetOnStageQuestions().Single();
        Assert.That(question.Nickname, Is.EqualTo("Alice"));
        Assert.That(question.SourceRoom, Is.EqualTo("%#The\\bLobby"));
        Assert.That(question.Message, Is.EqualTo("Question?"));
        Assert.That(hostMessages.Any(m => m.Contains(" 806 ") &&
                                          m.Contains("%#Stage 1 Alice %#The\\bLobby :Question?")), Is.True);
        Assert.That(spectatorMessages.Any(m => m.Contains(" 442 ")), Is.False);
        Assert.That(hostMessages.Concat(spectatorMessages).Any(m => m.Contains(" EQUESTION ")), Is.False);
    }

    [Test]
    public void EventList_ReturnsPendingQuestionsToHost()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.AddOnStageQuestion("Alice", "%#Stage2", "Question?");
        Clear(hostMessages);

        new EventCommand().Execute(CreateChatFrame(server.Object, host.Object, "LIST", "%#Stage"));

        Assert.That(hostMessages.Any(m => m.Contains(" 808 ")), Is.True);
        Assert.That(hostMessages.Any(m => m.Contains(" 809 ") &&
                                          m.Contains("%#Stage 1 Alice %#Stage2 :Question?")), Is.True);
        Assert.That(hostMessages.Any(m => m.Contains(" 810 ")), Is.True);
    }

    [Test]
    public void EventList_RejectsSpectators()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var spectatorMessages = new List<string>();
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        channel.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        channel.AddOnStageQuestion("Bob", "%#Stage", "Question?");
        Clear(spectatorMessages);

        new EventCommand().Execute(CreateChatFrame(server.Object, spectator.Object, "LIST", "%#Stage"));

        Assert.That(spectatorMessages.Any(m => m.Contains(" 482 ")), Is.True);
        Assert.That(spectatorMessages.Any(m => m.Contains(" 809 ")), Is.False);
    }

    [Test]
    public void EventAdd_AddsImportedQuestionWithSourceRoomAndNotifiesHosts()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        Clear(hostMessages);

        new EventCommand().Execute(
            CreateChatFrame(server.Object, host.Object, "ADD", "%#Stage", "Alice", "%#Stage2", "Question?"));

        var question = channel.GetOnStageQuestions().Single();
        Assert.That(question.Nickname, Is.EqualTo("Alice"));
        Assert.That(question.SourceRoom, Is.EqualTo("%#Stage2"));
        Assert.That(question.Message, Is.EqualTo("Question?"));
        Assert.That(hostMessages.Any(m => m.Contains(" 806 ") &&
                                          m.Contains("%#Stage 1 Alice %#Stage2 :Question?")), Is.True);
    }

    [Test]
    public void EventDelete_RemovesPendingQuestionAndNotifiesHosts()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.AddOnStageQuestion("Alice", "%#Stage", "Question?");
        Clear(hostMessages);

        new EventCommand().Execute(CreateChatFrame(server.Object, host.Object, "DELETE", "%#Stage", "1"));

        Assert.That(channel.GetOnStageQuestions(), Is.Empty);
        Assert.That(hostMessages.Any(m => m.Contains(" 807 ") &&
                                          m.Contains("%#Stage 1 Alice %#Stage :Question?")), Is.True);
    }

    [Test]
    public void Equestion_PreservesSourceRoom()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Equestion().Execute(
            CreateChatFrame(server.Object, host.Object, "%#Stage", "Alice", "%#Stage2", "Question?"));

        Assert.That(hostMessages.Concat(spectatorMessages).Any(m =>
            m.Contains(" EQUESTION %#Stage Alice %#Stage2 :Question?")), Is.True);
    }

    [Test]
    public void Equestion_RejectsSpectatorPublish()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Equestion().Execute(
            CreateChatFrame(server.Object, spectator.Object, "%#Stage", "Alice", "%#Stage", "Question?"));

        Assert.That(spectatorMessages.Any(m => m.Contains(" 482 ")), Is.True);
        Assert.That(hostMessages.Any(m => m.Contains(" EQUESTION ")), Is.False);
    }

    [Test]
    public void Equestion_RejectsPublishFromLinkedSourceRoom()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var stage = CreateOnStageChannel(channels);
        var source = new Channel("%#The\\bLobby");
        channels.Add(source);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        stage.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        source.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Equestion().Execute(
            CreateChatFrame(server.Object, spectator.Object, "%#Stage", "Alice", "%#The\\bLobby", "Question?"));

        Assert.That(spectatorMessages.Any(m => m.Contains(" 442 ")), Is.True);
        Assert.That(hostMessages.Any(m => m.Contains(" EQUESTION ")), Is.False);
    }

    [Test]
    public void Eprivmsg_RejectsSpectators()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Eprivmsg().Execute(CreateChatFrame(server.Object, spectator.Object, "%#Stage", "Hello"));

        Assert.That(spectatorMessages.Any(m => m.Contains(" 404 ")), Is.True);
        Assert.That(hostMessages.Any(m => m.Contains(" EPRIVMSG ")), Is.False);
    }

    [Test]
    public void Eprivmsg_BroadcastsFromSpeakerToWholeChannel()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var spectatorMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var spectator = CreateUser("Alice", server.Object, spectatorMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(spectator.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, spectatorMessages);

        new Eprivmsg().Execute(CreateChatFrame(server.Object, host.Object, "%#Stage", "On stage now"));

        // A speaker's EPRIVMSG (the guest/host on-stage message) reaches everyone,
        // spectators included.
        Assert.That(hostMessages.Any(m => m.Contains(" 404 ")), Is.False);
        Assert.That(hostMessages.Any(m => m.Contains(" EPRIVMSG %#Stage :On stage now")), Is.True);
        Assert.That(spectatorMessages.Any(m => m.Contains(" EPRIVMSG %#Stage :On stage now")), Is.True);
    }

    [Test]
    public void Join_HidesSpectatorArrivalFromOtherSpectators()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var aliceMessages = new List<string>();
        var bobMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var alice = CreateUser("Alice", server.Object, aliceMessages);
        var bob = CreateUser("Bob", server.Object, bobMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(alice.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, aliceMessages, bobMessages);

        channel.Join(bob.Object, EnumChannelAccessResult.SUCCESS_GUEST);

        // Hosts see every arrival; a spectator never sees another spectator arrive.
        Assert.That(hostMessages.Any(m => m.Contains(" JOIN ") && m.Contains("Bob")), Is.True);
        Assert.That(aliceMessages.Any(m => m.Contains("Bob")), Is.False);
    }

    [Test]
    public void Part_HidesSpectatorDepartureFromOtherSpectators()
    {
        var channels = new List<IChannel>();
        var server = CreateServer(channels);
        var channel = CreateOnStageChannel(channels);
        var hostMessages = new List<string>();
        var aliceMessages = new List<string>();
        var bobMessages = new List<string>();
        var host = CreateUser("Host", server.Object, hostMessages);
        var alice = CreateUser("Alice", server.Object, aliceMessages);
        var bob = CreateUser("Bob", server.Object, bobMessages);
        channel.Join(host.Object, EnumChannelAccessResult.SUCCESS_HOST);
        channel.Join(alice.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        channel.Join(bob.Object, EnumChannelAccessResult.SUCCESS_GUEST);
        Clear(hostMessages, aliceMessages, bobMessages);

        channel.Part(bob.Object);

        // Hosts see every departure; a spectator never sees another spectator leave.
        Assert.That(hostMessages.Any(m => m.Contains(" PART ") && m.Contains("Bob")), Is.True);
        Assert.That(aliceMessages.Any(m => m.Contains("Bob")), Is.False);
    }

    private static Channel CreateOnStageChannel(List<IChannel> channels)
    {
        var channel = new Channel("%#Stage");
        channel.Modes.Auditorium.ModeValue = true;
        channel.Modes.OnStage.ModeValue = true;
        channels.Add(channel);
        return channel;
    }

    private static Mock<IServer> CreateServer(List<IChannel> channels)
    {
        var server = new Mock<IServer>();
        server.Setup(s => s.ToString()).Returns("MockServer");
        server.Setup(s => s.Name).Returns("MockServer");
        server.Setup(s => s.MaxChannels).Returns(10);
        server.Setup(s => s.JoinOnCreate).Returns(true);
        server.Setup(s => s.GetChannels()).Returns(channels);
        server.Setup(s => s.GetChannelByName(It.IsAny<string>()))
            .Returns((string name) => channels.FirstOrDefault(c =>
                string.Equals(c.GetName(), name, StringComparison.OrdinalIgnoreCase)));
        server.Setup(s => s.CreateChannel(It.IsAny<string>()))
            .Returns((string name) => new Channel(name));
        server.Setup(s => s.CreateChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string name, string topic, string key) => new Channel(name).UpdateTopic(topic));
        server.Setup(s => s.AddChannel(It.IsAny<IChannel>()))
            .Callback((IChannel channel) => channels.Add(channel))
            .Returns(true);
        server.Setup(s => s.RemoveChannel(It.IsAny<IChannel>()))
            .Callback((IChannel channel) => channels.Remove(channel));
        return server;
    }

    private static Mock<IUser> CreateUser(string nickname, IServer server, List<string> messages)
    {
        var channels = new Dictionary<IChannel, IChannelMember>();
        var protocol = CreateProtocol();
        var address = CreateAddress(nickname);
        var user = new Mock<IUser>();
        user.Setup(u => u.Server).Returns(server);
        user.Setup(u => u.Name).Returns(nickname);
        user.Setup(u => u.Nickname).Returns(nickname);
        user.Setup(u => u.ToString()).Returns(nickname);
        user.Setup(u => u.GetAddress()).Returns(address);
        user.Setup(u => u.GetProtocol()).Returns(protocol.Object);
        user.Setup(u => u.GetChannels()).Returns(channels);
        user.Setup(u => u.AddChannel(It.IsAny<IChannel>(), It.IsAny<IChannelMember>()))
            .Callback((IChannel channel, IChannelMember member) => channels[channel] = member);
        user.Setup(u => u.RemoveChannel(It.IsAny<IChannel>()))
            .Callback((IChannel channel) => channels.Remove(channel));
        user.Setup(u => u.IsOn(It.IsAny<IChannel>()))
            .Returns((IChannel channel) => channels.ContainsKey(channel));
        user.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        user.Setup(u => u.IsAdministrator()).Returns(false);
        user.Setup(u => u.IsRegistered()).Returns(true);
        user.Setup(u => u.GetSspiHandler()).Returns((ISaslHandler?)null);
        user.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string message) => messages.Add(message));
        return user;
    }

    private static UserObject CreateRealUser(string nickname, IServer server, List<string> messages)
    {
        var connection = new Mock<IConnection>();
        var protocol = CreateProtocol();
        var dataRegulator = new Mock<IDataRegulator>();
        var floodProtection = new Mock<IFloodProtectionProfile>();
        var saslHandler = new Mock<ISaslHandler>();
        connection.Setup(c => c.GetIp()).Returns("127.0.0.1");
        dataRegulator.Setup(d => d.PushOutgoing(It.IsAny<string>()))
            .Callback((string message) => messages.Add(message))
            .Returns((string message) => message.Length);

        var user = new UserObject(
            connection.Object,
            protocol.Object,
            dataRegulator.Object,
            floodProtection.Object,
            server,
            _ => saslHandler.Object);
        user.Nickname = nickname;
        user.UserAddress.User = nickname.ToLowerInvariant();
        user.UserAddress.Host = "host.local";
        user.Register();
        return user;
    }

    private static Mock<IProtocol> CreateProtocol()
    {
        var protocol = new Mock<IProtocol>();
        protocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC8);
        protocol.Setup(p => p.GetFormat(It.IsAny<IUser>()))
            .Returns((IUser user) => user.GetAddress().Nickname);
        protocol.Setup(p => p.FormattedUser(It.IsAny<IChannelMember>()))
            .Returns((IChannelMember member) =>
            {
                var mode = member.Owner.ModeValue
                    ? "."
                    : member.Operator.ModeValue
                        ? "@"
                        : member.Voice.ModeValue
                            ? "+"
                            : string.Empty;
                return $"{mode}{member.GetUser().GetAddress().Nickname}";
            });
        return protocol;
    }

    private static IUserAddress CreateAddress(string nickname)
    {
        var address = new UserAddress
        {
            User = nickname.ToLowerInvariant(),
            Host = "host.local"
        };
        address.SetNickname(nickname);
        return address;
    }

    private static IChatFrame CreateChatFrame(IServer server, IUser user, params string[] parameters)
    {
        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(parameters.ToList());

        var chatFrame = new Mock<IChatFrame>();
        chatFrame.Setup(f => f.Server).Returns(server);
        chatFrame.Setup(f => f.User).Returns(user);
        chatFrame.Setup(f => f.ChatMessage).Returns(chatMessage.Object);
        return chatFrame.Object;
    }

    private static void Clear(params List<string>[] messageLists)
    {
        foreach (var messages in messageLists)
        {
            messages.Clear();
        }
    }
}
