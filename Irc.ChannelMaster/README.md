# ChannelMaster

`ChannelMaster` is a new coordination service for IRC7 with two runtime roles:

- `Controller` process (single leader): channel arbitration and broadcast assignment.
- `Broadcast` process (many workers): receives assignments and serves channel updates.

This initial implementation focuses on internal coordination primitives and a runnable process skeleton.

## Current capabilities

- Controller lease election (`cm:controller:lease`).
- Cluster convergence membership heartbeat (`cm:cluster:masters`).
- Leader heartbeat and definition (`cm:cluster:leader`).
- Leader election by maximum ChannelMaster ID when no leader is detected.
- Broadcast worker heartbeats (`cm:broadcast:*`).
- Chat server heartbeats (`cm:chat:*`).
- Chat-server-to-broadcast assignments (`cm:assign:chat-to-broadcast`).
- Strict, canonical channel claim (`cm:channels`, case-insensitive via uppercase normalization).

## Convergence defaults

- Leader heartbeat interval: `10s`
- Leader poll rounds: `5`
- Delay between polls: `3s`
- Missed leader heartbeats before re-election: `6`

## Run

From repository root:

```sh
dotnet run --project Irc.ChannelMaster/Irc.ChannelMaster.csproj -- --once
```

Controller-only:

```sh
dotnet run --project Irc.ChannelMaster/Irc.ChannelMaster.csproj -- --mode controller --once
```

Broadcast-only:

```sh
dotnet run --project Irc.ChannelMaster/Irc.ChannelMaster.csproj -- --mode broadcast --worker-load 12 --once
```

Redis-backed state:

```sh
dotnet run --project Irc.ChannelMaster/Irc.ChannelMaster.csproj -- --store redis --redis "localhost:6379" --once
```

## Test

```sh
dotnet test Irc.ChannelMaster.Tests/Irc.ChannelMaster.Tests.csproj --nologo
```

