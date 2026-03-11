# Copilot Instructions for IRC7

## Project Overview

IRC7 is an IRC server inspired by MSN Chat, implementing the **RFC 1459 IRC protocol** and the **IRCX protocol extensions**. It is written in **C# targeting .NET 10** and is designed to support distributed deployments via Redis pub/sub. The project is hosted live by SkyCrest.

### Protocol References (Primary Sources)

All protocol behaviour must be validated against the following canonical documents stored in the `docs/` directory:

- **`docs/rfc1459.txt`** — RFC 1459: Internet Relay Chat Protocol (baseline IRC)
- **`docs/draft-pfenning-irc-extensions-04.txt`** — IRCX Extensions draft (MSN Chat extensions)

If a protocol detail cannot be confirmed from these documents, **escalate to a human maintainer** rather than guessing.

---

## Repository Structure

```
irc7/
├── Irc/                        # Core IRC protocol library (main logic)
│   ├── Commands/               # IRC/IRCX command handlers (~51 commands)
│   ├── Modes/                  # Channel and user mode logic
│   │   ├── Channel/            # Channel modes + ModeEngine, ModeRule, ModeOperation
│   │   └── User/               # User modes (e.g., ServerNotice)
│   ├── Objects/                # Core domain objects
│   │   ├── Channel/            # Channel, ChannelModes, ChannelProps, ChannelAccess, InMemoryChannel
│   │   ├── Member/             # ChannelMember and member-level modes
│   │   ├── Server/             # Server, ServerModes, ServerAccess, ServerHandlers
│   │   ├── User/               # User object
│   │   └── Collections/        # Collection helpers
│   ├── Protocols/              # Protocol version implementations
│   │   ├── Protocol.cs         # Base class with command registry
│   │   ├── Irc.cs              # RFC 1459 IRC protocol (~50 commands registered)
│   │   ├── IrcX.cs             # IRCX extensions (inherits from Irc.cs)
│   │   └── Irc3.cs – Irc8.cs  # Protocol version variants
│   ├── Security/               # Authentication/SASL packages
│   │   ├── Packages/           # ANON, GateKeeper, NTLM, PassportV4
│   │   └── SecurityManager.cs  # SASL package manager
│   ├── Services/               # CacheManager (Redis), DataStore (in-memory)
│   └── IO/                     # DataStore for file-backed key-value storage
├── Irc.Daemon/                 # Executable: IRC server daemon
│   ├── Program.cs              # Entry point + CLI argument parsing
│   ├── SocketServer.cs         # TCP listener (inherits Socket)
│   ├── SocketConnection.cs     # Per-client connection handler
│   ├── DefaultServer.json      # Server configuration (name, SASL, limits, etc.)
│   ├── DefaultChannels.json    # Pre-configured channels loaded at startup
│   └── DefaultCredentials.json # Default user credentials
├── Irc.Directory/              # Abstract Chat Service (ACS) directory server
│   ├── DirectoryServer.cs      # Room discovery, routing, failover, load balancing
│   └── Commands/               # ACS-specific commands: Create, Finds, Nick, Ircvers
├── Irc.Helpers/                # Utility/extension classes
│   ├── StringExtensions.cs
│   ├── ByteExtensions.cs
│   ├── GuidExtensions.cs
│   ├── RegularExpressions.cs   # IRC parsing regex patterns
│   ├── Base64.cs
│   └── SerializationExtensions.cs
├── Irc.Logging/                # NLog configuration (Logging.Attach())
├── Irc.Tests/                  # NUnit test suite for Irc core
│   ├── Commands/               # Command tests (Create, Listx)
│   ├── Infrastructure/         # Repository tests
│   ├── Objects/                # Channel, Member, AccessList tests
│   └── Services/               # ACS mapping tests
├── SSPI.GateKeeper/            # GateKeeper token auth package
├── SSPI.GateKeeper.Tests/      # GateKeeper tests
├── SSPI.NTLM/                  # Full NTLM (Windows auth) implementation with DES crypto
├── SSPI.NTLM.Tests/            # NTLM tests
├── docs/                       # Protocol specification documents
│   ├── rfc1459.txt
│   └── draft-pfenning-irc-extensions-04.txt
├── Dockerfile                  # Multi-stage .NET 10 build → runtime-deps image
├── docker-compose.yml          # IRC7 daemon + KeyDB (Redis-compatible) on port 7001
├── entrypoint.sh               # Docker entrypoint (Redis mode / Server mode)
├── Directory.Build.props       # Shared MSBuild: LangVersion=latest, net10.0
├── Irc7.sln                    # Visual Studio solution
└── Notes.md                    # Developer notes (NTLM workarounds — see below)
```

---

## Build, Test, and Run

### Prerequisites

- .NET 10 SDK (`dotnet --version` should show `10.x.x`)
- (Optional) Docker + Docker Compose for running the full stack

### Build

```bash
# Always restore before building in a fresh clone
dotnet restore
dotnet build --no-restore
```

### Test

```bash
dotnet test --no-build --verbosity normal
```

All 33 tests should pass. There are currently 2 known benign build warnings:
- `CS8602` in `Irc/Commands/Create.cs` (possible null dereference)
- `CS8600` in `Irc.Tests/Commands/CreateTests.cs` (null assignment to non-nullable)

### Run (Docker)

```bash
docker-compose up
```

This starts:
- **IRC7 daemon** on port 6667 (default)
- **KeyDB** (Redis-compatible) on port 7001

### Run Locally

```bash
dotnet run --project Irc.Daemon -- --type CS --ip 0.0.0.0 --port 6667 --fqdn irc.local
```

For a distributed deployment (ACS + CS):
```bash
# Start Abstract Chat Service (directory server)
dotnet run --project Irc.Daemon -- --type ACS --redis localhost:7001

# Start Chat Server (connects to ACS)
dotnet run --project Irc.Daemon -- --type CS --server localhost:6667
```

### Publish (Release)

```bash
dotnet publish Irc.Daemon --self-contained true /p:PublishTrimmed=false /p:PublishSingleFile=true -c Release -o ./output
```

---

## Key Architecture Concepts

### Protocol Hierarchy

```
Protocol (base)
  └── Irc            (RFC 1459 baseline)
        └── IrcX     (IRCX extensions)
              ├── Irc3 / Irc4 / Irc5 / Irc6 / Irc7 / Irc8  (version variants)
```

Each protocol class registers commands in its constructor. Adding a new command means:
1. Create a class in `Irc/Commands/` that inherits from `Command`
2. Override the `Execute` method
3. Register it in the appropriate protocol class (`Irc.cs`, `IrcX.cs`, etc.)

### Authentication (SASL Packages)

Four auth packages are supported, configured in `DefaultServer.json`:
- **ANON** — Anonymous (no password)
- **GateKeeper** — Custom token-based auth (`SSPI.GateKeeper/`)
- **NTLM** — Windows authentication (`SSPI.NTLM/` — see known issues)
- **Passport V4** — Microsoft Passport (`Irc/Security/Passport/PassportV4.cs`)

### Storage

| Layer | Class | Purpose |
|---|---|---|
| In-memory | `Irc/IO/DataStore.cs` | Key-value store, optional JSON file persistence |
| Distributed | `Irc/Services/CacheManager.cs` | Redis pub/sub via `StackExchange.Redis` |
| Config | `Irc.Daemon/Default*.json` | Server, channel, credential config loaded at startup |

### Distributed Architecture

- **ACS** (`Irc.Directory/`) acts as a room registry and load balancer
- **CS** daemons handle actual client connections and messaging
- Room state is shared via **Redis pub/sub** on the `irc7:channels` channel
- Failover: ACS detects dead CS servers and clones rooms to active servers

### Connection Tracking

`SocketServer` uses a `ConcurrentDictionary<BigInteger, ConcurrentDictionary<IConnection, byte>>` where `BigInteger` represents the hashed client IP address, enabling per-IP connection limits.

### Channel Member Levels

From highest to lowest privilege:
1. **Owner** (`~`) — full control
2. **Operator** (`@`) — can kick, ban, change modes
3. **Voice** (`+`) — can speak when channel is moderated
4. **Regular** — standard member

---

## Adding New Features

### Adding a New IRC Command

1. Create `Irc/Commands/YourCommand.cs` inheriting from `Command`
2. Implement `Execute(IChatFrame chatFrame)` (and optionally `Validate`)
3. Register in `Irc/Protocols/Irc.cs` (or `IrcX.cs` for IRCX-only commands):
   ```csharp
   Commands.Add("YOURCOMMAND", new YourCommand());
   ```
4. Add a test in `Irc.Tests/Commands/YourCommandTests.cs` following the pattern in `CreateTests.cs` or `ListxTests.cs`

### Adding a New Channel Mode

1. Create a class in `Irc/Modes/Channel/` implementing `IModeRule`
2. Register it in `ModeEngine` or the channel's mode list

### Adding a New User Mode

1. Create a class in `Irc/Modes/User/`
2. Register it in the appropriate user mode list

---

## Testing Patterns

Tests use **NUnit 4.2.2** with **Moq 4.20.72**. Key conventions:

- Mock `IServer`, `IChannel`, `IUser`, `IChatFrame` using Moq
- Use `InMemoryChannelRepository` for channel storage in tests
- Assert IRC protocol responses via mocked output (see `CreateTests.cs` for examples)
- Test method naming: `MethodName_Scenario_ExpectedResult` (e.g., `Execute_ShouldReturnError_WhenChannelNotFound`)

---

## Known Issues and Workarounds

### NTLM mIRC Compatibility (documented in `Notes.md`)

**Problem:** Certain NTLM challenge bytes cause authentication failures in mIRC.

**Workaround** (applied in `SSPI.NTLM`):
```csharp
for (var i = 0; i < challenge.Length; i++) challenge[i] = (char)(challenge[i] % 0x7F);
```
This ensures all challenge characters remain in the 7-bit ASCII range.

### Build: Always `dotnet restore` First

In a fresh clone, `dotnet build --no-restore` will fail because `obj/project.assets.json` files don't exist yet. Always run `dotnet restore` before `dotnet build --no-restore`.

### CI/CD Workflow Notes

The `.github/workflows/dotnet.yml` workflow:
- **build job**: Runs `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build` on every push/PR to `master`
- **image job**: Builds and pushes Docker image to `jyonxo/irc7d:latest` on `master` only (requires `DOCKER_USERNAME` and `DOCKER_TOKEN` secrets)

Workflow runs from forked PRs or Copilot agent branches may show `action_required` status — this is a standard GitHub security approval gate for first-time contributors, not a code failure.

---

## Configuration Reference

### `Irc.Daemon/DefaultServer.json`

Key settings:
```json
{
  "ServerID": "...",
  "ServerName": "...",
  "ServerTitle": "...",
  "MOTD": "...",
  "SaslPackages": ["ANON", "GateKeeper", "NTLM"],
  "PassportV4Secret": "...",
  "WebIrcWhitelist": [],
  "MaxChannels": 100,
  "MaxConnections": 1000,
  "PingInterval": 60,
  "MaxInputBytes": 1024,
  "MaxOutputBytes": 16384
}
```

### CLI Arguments (`Irc.Daemon/Program.cs`)

| Argument | Description |
|---|---|
| `--type <ACS\|CS>` | Server type: Abstract Chat Service or Chat Server |
| `--ip <address>` | Bind IP (default: `0.0.0.0`) |
| `--port <number>` | Port (default: `6667`) |
| `--fqdn <domain>` | Fully qualified domain name |
| `--server <ip:port>` | ACS server address (CS mode) |
| `--redis <conn>` | Redis connection string (ACS mode) |
| `--name <name>` | Override server name |

### Docker Environment Variables

| Variable | Description |
|---|---|
| `irc7d_type` | Server type (`ACS` or `CS`) |
| `irc7d_port` | Listening port |
| `irc7d_fqdn` | FQDN |
| `irc7d_server` | Chat server address |
| `irc7d_redis` | Redis URL (enables Redis/distributed mode) |

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `StackExchange.Redis` | 2.11.3 | Redis caching and pub/sub |
| `System.CommandLine` | 2.0.0-beta4 | CLI argument parsing |
| `NLog` | latest | Logging |
| `NUnit` | 4.2.2 | Test framework |
| `Moq` | 4.20.72 | Mocking in tests |
| `coverlet.collector` | latest | Code coverage |

---

## Maintainers

- [@jyonxo](https://github.com/jyonxo)
- [@realJoshByrnes](https://github.com/realJoshByrnes)
- [@joachimjusth](https://github.com/joachimjusth)
- [@ricardodevries](https://github.com/ricardodevries)
