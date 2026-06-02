using System.Text.Json.Serialization;
using Irc.Security;

namespace Irc.Directory.Daemon;

[JsonSerializable(typeof(Dictionary<string, PermissionProfile>))]
[JsonSerializable(typeof(PermissionProfile))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class DirectoryDaemonJsonContext : JsonSerializerContext
{
}

