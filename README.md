# IRC7

[![.NET](https://github.com/irc7-com/irc7/actions/workflows/dotnet.yml/badge.svg)](https://github.com/irc7-com/irc7/actions/workflows/dotnet.yml)

An IRC server inspired by MSN Chat, implementing IRC and IRCX protocols. Created by [@jyonxo](https://github.com/jyonxo) and [@realJoshByrnes](https://github.com/realJoshByrnes).

## Maintainers

<table>
  <tbody><tr>
    <td><a href="https://github.com/jyonxo"><img src="https://avatars.githubusercontent.com/u/20768067?s=120&v=4"><br><sub><b>jyonxo</b></sub></a></td>
    <td><a href="https://github.com/realJoshByrnes"><img src="https://avatars.githubusercontent.com/u/204185?s=120&v=4"><br><sub><b>realJoshByrnes</b></sub></a></td>
    <td><a href="https://github.com/joachimjusth"><img src="https://avatars.githubusercontent.com/u/3322377?s=120&v=4"><br><sub><b>joachimjusth</b></sub></a></td>
    <td><a href="https://github.com/ricardodevries"><img src="https://avatars.githubusercontent.com/u/78354174?s=120&v=4"><br><sub><b>ricardodevries</b></sub></a></td>
  </tr>
</tbody></table>

The current implementation of IRC7 is hosted by **SkyCrest**.

## Build

IRC7 targets **.NET 10** and is built using **GitHub Actions**. See the [workflow](.github/workflows/dotnet.yml) for details.

## Documentation

IRC7 is based on a mixture of the following specifications and data gathered during the time MSN Chat was operational:

- **RFC 1459 — Internet Relay Chat Protocol**: [original](https://www.rfc-editor.org/rfc/rfc1459.txt) · [local copy](docs/rfc1459.txt)
- **draft-pfenning-irc-extensions-04 — IRCX Extensions**: [original](https://www.ietf.org/archive/id/draft-pfenning-irc-extensions-04.txt) · [local copy](docs/draft-pfenning-irc-extensions-04.txt)

Canonical copies of these documents are stored in the [`docs/`](docs/) directory for reference.
