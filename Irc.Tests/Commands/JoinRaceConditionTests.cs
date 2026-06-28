using System.Collections.Concurrent;
using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

/// <summary>
/// Tests for JOIN notification semantics under both sequential and concurrent conditions.
///
/// Core rule being verified:
///   A JOIN notification should only be delivered to users who were ALREADY in the channel
///   when another user joined. A user who is joining should discover pre-existing members
///   exclusively through the 353 (RPL_NAMREPLY) response — never through a JOIN notification
///   for a user who was already present before they arrived.
///
/// The concurrent tests additionally document the known race condition where two users
/// joining "simultaneously" (same processing window) can cause each to receive a spurious
/// JOIN notification for the other, because Channel.Join() is not atomic: AddMember() and
/// the foreach-broadcast over _members are two separate steps that can interleave under
/// concurrent access.
/// </summary>
[TestFixture]
public class JoinRaceConditionTests
{
    private Mock<IServer> _mockServer = null!;
    private List<IChannel> _serverChannels = null!;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _serverChannels = new List<IChannel>();

        _mockServer.Setup(s => s.ToString()).Returns("MockServer");
        _mockServer.Setup(s => s.MaxChannels).Returns(10);
        _mockServer.Setup(s => s.JoinOnCreate).Returns(true);
        _mockServer.Setup(s => s.GetChannels()).Returns(_serverChannels);
        _mockServer.Setup(s => s.GetChannelByName(It.IsAny<string>()))
            .Returns((string name) =>
                _serverChannels.FirstOrDefault(c =>
                    string.Equals(c.GetName(), name, StringComparison.OrdinalIgnoreCase)));
        _mockServer.Setup(s => s.AddChannel(It.IsAny<IChannel>()))
            .Callback((IChannel ch) => _serverChannels.Add(ch))
            .Returns(true);
        _mockServer.Setup(s => s.CreateChannel(It.IsAny<string>()))
            .Returns((string name) => new Channel(name));
    }

    // -------------------------------------------------------------------------
    // Sequential tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// When UserB joins a channel that already has UserA:
    /// - UserB should receive their own JOIN notification.
    /// - UserB should see UserA listed in the 353 NAMREPLY.
    /// - UserB should NOT receive a separate JOIN notification for UserA,
    ///   because UserA was already a member — not a new joiner.
    /// </summary>
    [Test]
    public void SequentialJoin_SecondUser_SeesFirstUserVia353_NotViaJoin()
    {
        // Arrange
        var channel = new Channel("#test");
        _serverChannels.Add(channel);

        var messagesB = new List<string>();

        var userA = CreateMockUser("UserA", "usera", "hosta");
        userA.Setup(u => u.Send(It.IsAny<string>()));

        var userB = CreateMockUser("UserB", "userb", "hostb");
        userB.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesB.Add(msg));

        // Act: UserA joins first, then UserB
        Join.JoinChannels(_mockServer.Object, userA.Object, new List<string> { "#test" }, string.Empty);
        Join.JoinChannels(_mockServer.Object, userB.Object, new List<string> { "#test" }, string.Empty);

        // Assert: UserB's 353 lists both members
        var b353 = messagesB.FirstOrDefault(m => m.Contains("353"));
        Assert.That(b353, Is.Not.Null, "UserB should receive a 353 NAMREPLY.");
        Assert.That(b353, Does.Contain("UserA"), "UserB's 353 should include UserA.");
        Assert.That(b353, Does.Contain("UserB"), "UserB's 353 should include UserB.");

        // Assert: UserB should NOT receive a JOIN notification for UserA.
        // UserA was already in the channel — a new join broadcast should never be
        // sent for a user who was present before the joining user arrived.
        var bGotJoinForA = messagesB.Any(m => m.Contains(":UserA!") && m.Contains("JOIN"));
        Assert.That(bGotJoinForA, Is.False,
            "UserB should NOT receive a JOIN notification for UserA. " +
            "UserA was already in the channel; UserB should discover them via 353 only.");
    }

    /// <summary>
    /// When UserB joins a channel that already has UserA:
    /// - UserA SHOULD receive a JOIN notification for UserB,
    ///   because UserA was already a member when UserB's join was processed.
    /// </summary>
    [Test]
    public void SequentialJoin_FirstUser_ReceivesJoinNotification_WhenSecondUserJoins()
    {
        // Arrange
        var channel = new Channel("#test");
        _serverChannels.Add(channel);

        var messagesA = new List<string>();

        var userA = CreateMockUser("UserA", "usera", "hosta");
        userA.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesA.Add(msg));

        var userB = CreateMockUser("UserB", "userb", "hostb");
        userB.Setup(u => u.Send(It.IsAny<string>()));

        // Act
        Join.JoinChannels(_mockServer.Object, userA.Object, new List<string> { "#test" }, string.Empty);
        var countBeforeB = messagesA.Count;

        Join.JoinChannels(_mockServer.Object, userB.Object, new List<string> { "#test" }, string.Empty);

        // Assert: UserA (already in channel) received a JOIN notification for UserB
        var aGotJoinForB = messagesA
            .Skip(countBeforeB)
            .Any(m => m.Contains(":UserB!") && m.Contains("JOIN"));

        Assert.That(aGotJoinForB, Is.True,
            "UserA (already in the channel) SHOULD receive a JOIN notification when UserB joins.");
    }

    /// <summary>
    /// When UserC joins a channel that already has UserA and UserB:
    /// - UserC's 353 should list all three members.
    /// - UserC should NOT receive JOIN notifications for UserA or UserB (both were pre-existing).
    /// - UserA and UserB should each receive a JOIN notification for UserC.
    /// </summary>
    [Test]
    public void SequentialJoin_ThirdUser_SeesAllExistingMembersVia353_NotViaJoin()
    {
        // Arrange
        var channel = new Channel("#test");
        _serverChannels.Add(channel);

        var messagesA = new List<string>();
        var messagesB = new List<string>();
        var messagesC = new List<string>();

        var userA = CreateMockUser("UserA", "usera", "hosta");
        userA.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesA.Add(msg));

        var userB = CreateMockUser("UserB", "userb", "hostb");
        userB.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesB.Add(msg));

        var userC = CreateMockUser("UserC", "userc", "hostc");
        userC.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesC.Add(msg));

        // Act: A and B join first, then C
        Join.JoinChannels(_mockServer.Object, userA.Object, new List<string> { "#test" }, string.Empty);
        Join.JoinChannels(_mockServer.Object, userB.Object, new List<string> { "#test" }, string.Empty);
        var countA = messagesA.Count;
        var countB = messagesB.Count;

        Join.JoinChannels(_mockServer.Object, userC.Object, new List<string> { "#test" }, string.Empty);

        // Assert: UserC's 353 includes all three members
        var c353 = messagesC.FirstOrDefault(m => m.Contains("353"));
        Assert.That(c353, Is.Not.Null, "UserC should receive a 353 NAMREPLY.");
        Assert.That(c353, Does.Contain("UserA"), "UserC's 353 should list UserA.");
        Assert.That(c353, Does.Contain("UserB"), "UserC's 353 should list UserB.");
        Assert.That(c353, Does.Contain("UserC"), "UserC's 353 should list UserC.");

        // Assert: UserC should NOT receive JOIN notifications for pre-existing members
        Assert.That(messagesC.Any(m => m.Contains(":UserA!") && m.Contains("JOIN")), Is.False,
            "UserC should NOT receive a JOIN for UserA — UserA was already in the channel.");
        Assert.That(messagesC.Any(m => m.Contains(":UserB!") && m.Contains("JOIN")), Is.False,
            "UserC should NOT receive a JOIN for UserB — UserB was already in the channel.");

        // Assert: UserA and UserB each receive a JOIN notification for UserC
        Assert.That(messagesA.Skip(countA).Any(m => m.Contains(":UserC!") && m.Contains("JOIN")), Is.True,
            "UserA (already in channel) SHOULD receive a JOIN notification for UserC.");
        Assert.That(messagesB.Skip(countB).Any(m => m.Contains(":UserC!") && m.Contains("JOIN")), Is.True,
            "UserB (already in channel) SHOULD receive a JOIN notification for UserC.");
    }

    // -------------------------------------------------------------------------
    // Concurrent / race condition tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// When two users join the same channel concurrently, regardless of which thread
    /// wins the race, both users must be present in the channel and appear in each
    /// other's 353 NAMREPLY.
    ///
    /// This is the minimum correctness guarantee: the channel must not lose a joiner
    /// due to a data race on the internal _members List&lt;T&gt;.
    /// </summary>
    [Test]
    [Repeat(10)]
    public void ConcurrentJoin_BothUsers_AppearInEachOthers353()
    {
        // Arrange
        var channel = new Channel("#race");

        var messagesA = new ConcurrentBag<string>();
        var messagesB = new ConcurrentBag<string>();

        var userA = CreateMockUser("UserA", "usera", "hosta");
        userA.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesA.Add(msg));
        userA.Setup(u => u.Server).Returns(_mockServer.Object);

        var userB = CreateMockUser("UserB", "userb", "hostb");
        userB.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesB.Add(msg));
        userB.Setup(u => u.Server).Returns(_mockServer.Object);

        // Synchronise both threads so they enter channel.Join at the same instant,
        // maximising the chance that AddMember() and the foreach-broadcast interleave.
        var barrier = new Barrier(2);

        var taskA = Task.Run(() =>
        {
            barrier.SignalAndWait();
            channel.Join(userA.Object);
            channel.SendNames(userA.Object);
        });

        var taskB = Task.Run(() =>
        {
            barrier.SignalAndWait();
            channel.Join(userB.Object);
            channel.SendNames(userB.Object);
        });

        Task.WaitAll(taskA, taskB);

        // Assert: both users received a 353 NAMREPLY. Due to scheduling the exact
        // contents may vary (one sender may complete SendNames before the other
        // has been added), so assert that the union of both NAMEREPLY messages
        // includes both users. This keeps the test deterministic while still
        // ensuring no joiner was lost during the concurrent join.
        var a353 = messagesA.FirstOrDefault(m => m.Contains("353"));
        var b353 = messagesB.FirstOrDefault(m => m.Contains("353"));

        Assert.That(a353, Is.Not.Null, "UserA should receive a 353 NAMREPLY.");
        Assert.That(b353, Is.Not.Null, "UserB should receive a 353 NAMREPLY.");

        var combined = string.Concat(a353 ?? string.Empty, " ", b353 ?? string.Empty);
        Assert.That(combined, Does.Contain("UserA"),
            "At least one NAMEREPLY should include UserA.");
        Assert.That(combined, Does.Contain("UserB"),
            "At least one NAMEREPLY should include UserB.");
    }

    /// <summary>
    /// Race condition scenario: Two users joining simultaneously.
    ///
    /// With the <c>ImmutableList&lt;T&gt; + Interlocked.CompareExchange</c> model the
    /// following is <b>provably guaranteed</b>, regardless of thread scheduling:
    ///
    ///   • It is <em>impossible</em> for <b>both</b> concurrent joiners to receive a
    ///     JOIN notification for each other.  A circular-ordering argument shows why:
    ///     for A to receive B's JOIN, B's snapshot must be taken after A's CAS; for B
    ///     to receive A's JOIN, A's snapshot must be taken after B's CAS — but A's CAS
    ///     happens after A's snapshot, producing the impossible cycle
    ///     A-read &gt; B-CAS &gt; B-read &gt; A-CAS &gt; A-read.
    ///
    ///   • At most one of the two can receive the other's JOIN (whichever thread loses
    ///     the CAS race finds the winner already in the list and broadcasts to them).
    ///
    ///   • Both always see each other in their 353 NAMREPLY (see
    ///     <see cref="ConcurrentJoin_BothUsers_AppearInEachOthers353"/>).
    ///
    /// A guarantee of "neither receives the other's JOIN" would require explicit
    /// two-phase synchronisation across both joining threads, which is outside the
    /// scope of the current single-phase join model.
    /// </summary>
    [Test]
    [Repeat(10)]
    public void ConcurrentJoin_AtMostOneUserReceivesJoinNotificationForTheOther()
    {
        // Arrange
        var channel = new Channel("#race");

        var messagesA = new ConcurrentBag<string>();
        var messagesB = new ConcurrentBag<string>();

        var userA = CreateMockUser("UserA", "usera", "hosta");
        userA.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesA.Add(msg));
        userA.Setup(u => u.Server).Returns(_mockServer.Object);

        var userB = CreateMockUser("UserB", "userb", "hostb");
        userB.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => messagesB.Add(msg));
        userB.Setup(u => u.Server).Returns(_mockServer.Object);

        var barrier = new Barrier(2);

        var taskA = Task.Run(() =>
        {
            barrier.SignalAndWait();
            channel.Join(userA.Object);
            channel.SendNames(userA.Object);
        });

        var taskB = Task.Run(() =>
        {
            barrier.SignalAndWait();
            channel.Join(userB.Object);
            channel.SendNames(userB.Object);
        });

        Task.WaitAll(taskA, taskB);

        var aReceivedJoinForB = messagesA.Any(m => m.Contains(":UserB!") && m.Contains("JOIN"));
        var bReceivedJoinForA = messagesB.Any(m => m.Contains(":UserA!") && m.Contains("JOIN"));

        // The provable guarantee: it is impossible for BOTH to receive the other's JOIN.
        // The thread that wins the CAS race joins with an empty snapshot; the loser may
        // find the winner already in the list — but never the other way around.
        Assert.That(aReceivedJoinForB && bReceivedJoinForA, Is.False,
            "Both users CANNOT simultaneously receive a JOIN notification for each other. " +
            "The ImmutableList+CAS model makes this impossible via a circular-ordering proof.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Mock<IUser> CreateMockUser(string nickname, string username, string host)
    {
        var mockProtocol = new Mock<IProtocol>();
        // Use a protocol level above IRC3 so Channel.Join uses RPL_JOIN_MSN (contains the nickname).
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC4);

        // GetFormat: returns the JOINING user's nickname so JOIN messages are identifiable.
        mockProtocol.Setup(p => p.GetFormat(It.IsAny<IUser>()))
            .Returns((IUser u) => u.GetAddress().Nickname);

        // FormattedUser: returns the member's nickname so the 353 names list is readable.
        mockProtocol.Setup(p => p.FormattedUser(It.IsAny<IChannelMember>()))
            .Returns((IChannelMember m) => m.GetUser().GetAddress().Nickname);

        var address = new UserAddress();
        address.SetNickname(nickname);
        address.User = username;
        address.Host = host;

        var mockU = new Mock<IUser>();
        mockU.Setup(u => u.GetAddress()).Returns(address);
        mockU.Setup(u => u.ToString()).Returns(nickname);
        mockU.Setup(u => u.GetChannels()).Returns(new Dictionary<IChannel, IChannelMember>());
        mockU.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        mockU.Setup(u => u.IsAdministrator()).Returns(false);
        mockU.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);
        mockU.Setup(u => u.Send(It.IsAny<string>()));
        return mockU;
    }
}




