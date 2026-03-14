using Irc.ChannelMaster.Controller;
using Irc.ChannelMaster.Gateway;
using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;

namespace Irc.ChannelMaster.Tests.Controller;

public class ControllerProcessTests
{
    private sealed class AcceptAllGateway : IChatServerGateway
    {
        public Task<bool> SendAssignAsync(string chatServerId, string channelName, string channelUid, TimeSpan ttl, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class RejectAllGateway : IChatServerGateway
    {
        public Task<bool> SendAssignAsync(string chatServerId, string channelName, string channelUid, TimeSpan ttl, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class RejectThenAcceptGateway : IChatServerGateway
    {
        private readonly HashSet<string> _busyServers;

        public RejectThenAcceptGateway(params string[] busyServers)
        {
            _busyServers = new HashSet<string>(busyServers, StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> SendAssignAsync(string chatServerId, string channelName, string channelUid, TimeSpan ttl, CancellationToken cancellationToken = default)
            => Task.FromResult(!_busyServers.Contains(chatServerId));
    }

    private static readonly IChatServerGateway DefaultGateway = new AcceptAllGateway();

    [Test]
    public async Task RunOnce_AssignsChatServersAcrossBroadcastWorkers()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatBroadcastWorkerAsync("worker-1", 0, TimeSpan.FromMinutes(1));
        await store.HeartbeatBroadcastWorkerAsync("worker-2", 0, TimeSpan.FromMinutes(1));

        await store.HeartbeatChatServerAsync("chat-1", "chat1.example.com", 5, 1, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("chat-2", "chat2.example.com", 3, 1, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("chat-3", "chat3.example.com", 4, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        var ranAsLeader = await controller.RunOnceAsync();
        var assignments = await store.GetChatServerAssignmentsAsync();

        Assert.That(ranAsLeader, Is.True);
        Assert.That(assignments.Count, Is.EqualTo(3));
        Assert.That(assignments.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task RunOnce_DoesNotActAsLeader_WhenControllerLeaseIsHeldByAnotherInstance()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.TryAcquireControllerLeaseAsync("controller-B", TimeSpan.FromMinutes(1));

        var ranAsLeader = await controller.RunOnceAsync();

        Assert.That(ranAsLeader, Is.False);
        Assert.That(controller.IsLeader, Is.False);
    }

    [Test]
    public async Task CreateChannelAsync_AssignsLeastLoadedChatServer_AndClaimsCaseInsensitiveName()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-2", "acs2.example.com", 5, 1, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();

        var before = DateTime.UtcNow;
        var first = await controller.CreateChannelAsync("%#General");
        var after = DateTime.UtcNow;
        var second = await controller.CreateChannelAsync("%#general");
        var record = await controller.GetChannelRecordAsync("%#GENERAL");

        Assert.That(first.Status, Is.EqualTo(CreateChannelStatus.Success));
        Assert.That(first.ServerId, Is.EqualTo("acs-1"));
        Assert.That(first.ChannelUid, Is.Not.Null);
        Assert.That(first.ChannelUid, Does.StartWith("acs-1:"));
        Assert.That(second.Status, Is.EqualTo(CreateChannelStatus.NameConflict));
        Assert.That(record, Is.Not.Null);
        Assert.That(record!.OwnerServerId, Is.EqualTo("acs-1"));
        Assert.That(record.ChannelUid, Is.EqualTo(first.ChannelUid));
        Assert.That(record.ChannelName, Is.EqualTo("%#General"));
        Assert.That(record.CreatedUtc, Is.InRange(before, after));
    }

    [Test]
    public async Task RunOnce_ElectsMaximumIdAsLeader_WhenNoLeaderExists()
    {
        var store = new InMemoryChannelMasterStore();
        var low = new ControllerProcess(store, DefaultGateway, "cm-01") { LeaderPollRepeats = 1, LeaderPollInterval = TimeSpan.Zero };
        var high = new ControllerProcess(store, DefaultGateway, "cm-99") { LeaderPollRepeats = 1, LeaderPollInterval = TimeSpan.Zero };

        await store.HeartbeatChannelMasterAsync("cm-01", TimeSpan.FromMinutes(1));
        await store.HeartbeatChannelMasterAsync("cm-99", TimeSpan.FromMinutes(1));

        var lowLeader = await low.RunOnceAsync();
        var highLeader = await high.RunOnceAsync();
        var clusterLeader = await store.GetCurrentLeaderAsync();

        Assert.That(lowLeader, Is.False);
        Assert.That(highLeader, Is.True);
        Assert.That(clusterLeader, Is.EqualTo("cm-99"));
    }

    [Test]
    public async Task HandleCommandAsync_Create_ReturnsBusy_WhenNoChatServersAreAvailable()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        _ = await controller.RunOnceAsync();
        var response = await controller.HandleCommandAsync("CREATE", new[] { "%#General" });

        Assert.That(response.Status, Is.EqualTo("BUSY"));
        Assert.That(response.Arguments, Is.Empty);
    }

    [Test]
    public async Task HandleCommandAsync_Create_ReturnsSuccessWithAssignedServerIdAndChannelUid()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-2", "acs2.example.com", 3, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();
        var response = await controller.HandleCommandAsync("CREATE", new[] { "%#General" });

        Assert.That(response.Status, Is.EqualTo("SUCCESS"));
        Assert.That(response.Arguments.Count, Is.EqualTo(2));
        Assert.That(response.Arguments[0], Is.EqualTo("acs-1"));
        Assert.That(response.Arguments[1], Does.StartWith("acs-1:"));
        Assert.That(response.ToProtocolString(), Does.StartWith("SUCCESS acs-1 acs-1:"));
    }

    [Test]
    public async Task RunOnce_ReconcilesAssignmentsThatPointToExpiredWorkers()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("chat-1", "chat1.example.com", 3, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        await store.HeartbeatBroadcastWorkerAsync("worker-stale", 0, TimeSpan.FromMilliseconds(1));
        await store.SetChatServerAssignmentAsync("chat-1", "worker-stale");

        await Task.Delay(20);

        _ = await controller.RunOnceAsync();
        var assignments = await store.GetChatServerAssignmentsAsync();

        Assert.That(assignments, Is.Empty);
    }

    [Test]
    public async Task CreateChannelAsync_TriesNextServer_WhenFirstServerIsBusy()
    {
        var store = new InMemoryChannelMasterStore();
        var gateway = new RejectThenAcceptGateway("acs-1"); // acs-1 is BUSY
        var controller = new ControllerProcess(store, gateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("acs-2", "acs2.example.com", 5, 1, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();
        var result = await controller.CreateChannelAsync("%#Lobby");

        Assert.That(result.Status, Is.EqualTo(CreateChannelStatus.Success));
        Assert.That(result.ServerId, Is.EqualTo("acs-2"));
        Assert.That(result.ChannelUid, Does.StartWith("acs-2:"));
    }

    [Test]
    public async Task CreateChannelAsync_ReturnsBusy_WhenAllServersRejectAssign()
    {
        var store = new InMemoryChannelMasterStore();
        var gateway = new RejectAllGateway();
        var controller = new ControllerProcess(store, gateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();
        var result = await controller.CreateChannelAsync("%#Lobby");

        Assert.That(result.Status, Is.EqualTo(CreateChannelStatus.Busy));
        // Channel claim should have been rolled back
        var record = await store.GetChannelRecordAsync("%#Lobby");
        Assert.That(record, Is.Null);
    }

    [Test]
    public async Task HandleCommandAsync_Assign_ReturnsSuccess_WhenChannelExistsAndServerAccepts()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();

        // First create a channel so it exists in the store
        var createResult = await controller.CreateChannelAsync("%#General");
        Assert.That(createResult.Status, Is.EqualTo(CreateChannelStatus.Success));

        // Now assign by UID
        var response = await controller.HandleCommandAsync("ASSIGN", new[] { createResult.ChannelUid! });

        Assert.That(response.Status, Is.EqualTo("SUCCESS"));
        Assert.That(response.Arguments.Count, Is.EqualTo(1));
        Assert.That(response.Arguments[0], Is.EqualTo("acs-1"));
    }

    [Test]
    public async Task HandleCommandAsync_Assign_ReturnsNotFound_WhenChannelUidDoesNotExist()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        _ = await controller.RunOnceAsync();
        var response = await controller.HandleCommandAsync("ASSIGN", new[] { "nonexistent:12345" });

        Assert.That(response.Status, Is.EqualTo("NOT FOUND"));
    }

    [Test]
    public async Task HandleCommandAsync_Assign_ReturnsBusy_WhenAllServersReject()
    {
        var store = new InMemoryChannelMasterStore();
        var acceptGateway = new AcceptAllGateway();
        var rejectGateway = new RejectAllGateway();

        // Create the channel with an accepting gateway
        var setupController = new ControllerProcess(store, acceptGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));
        _ = await setupController.RunOnceAsync();
        var createResult = await setupController.CreateChannelAsync("%#Lobby");
        Assert.That(createResult.Status, Is.EqualTo(CreateChannelStatus.Success));

        // Now try to ASSIGN with a rejecting gateway
        var controller = new ControllerProcess(store, rejectGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };
        _ = await controller.RunOnceAsync();

        var response = await controller.HandleCommandAsync("ASSIGN", new[] { createResult.ChannelUid! });

        Assert.That(response.Status, Is.EqualTo("BUSY"));
    }

    [Test]
    public async Task HandleCommandAsync_FindHost_ReturnsHostname_WhenChannelExists()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();
        await controller.CreateChannelAsync("%#General");

        var response = await controller.HandleCommandAsync("FINDHOST", new[] { "%#General" });

        Assert.That(response.Status, Is.EqualTo("SUCCESS"));
        Assert.That(response.Arguments.Count, Is.EqualTo(1));
        Assert.That(response.Arguments[0], Is.EqualTo("acs1.example.com"));
    }

    [Test]
    public async Task HandleCommandAsync_FindHost_ReturnsNotFound_WhenChannelDoesNotExist()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        _ = await controller.RunOnceAsync();
        var response = await controller.HandleCommandAsync("FINDHOST", new[] { "%#NonExistent" });

        Assert.That(response.Status, Is.EqualTo("NOT FOUND"));
    }

    [Test]
    public async Task HandleCommandAsync_FindHost_IsCaseInsensitive()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, DefaultGateway, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-1", "acs1.example.com", 1, 0, ChatServerStatusType.Active, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();
        await controller.CreateChannelAsync("%#General");

        var response = await controller.HandleCommandAsync("FINDHOST", new[] { "%#GENERAL" });

        Assert.That(response.Status, Is.EqualTo("SUCCESS"));
        Assert.That(response.Arguments[0], Is.EqualTo("acs1.example.com"));
    }
}
