using System.Text.Json.Serialization;
using Irc.Objects.Channel;
using Irc.Services;

namespace Irc;

/// <summary>
/// Source-generated JSON serializer context for AOT / trimming support in the Irc library.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(AcsRoomInfo))]
[JsonSerializable(typeof(AcsServerInfo))]
[JsonSerializable(typeof(InMemoryChannel))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool?))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class IrcJsonContext : JsonSerializerContext
{
}


