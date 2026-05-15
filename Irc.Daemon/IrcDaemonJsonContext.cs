using System.Text.Json.Serialization;
using Irc.Security;

namespace Irc7d;

/// <summary>
/// Source-generated JSON serializer context for AOT / trimming support in the Irc7d daemon.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, Credential>))]
[JsonSerializable(typeof(List<DefaultChannel>))]
[JsonSerializable(typeof(DefaultChannel))]
[JsonSerializable(typeof(Credential))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class IrcDaemonJsonContext : JsonSerializerContext
{
}

