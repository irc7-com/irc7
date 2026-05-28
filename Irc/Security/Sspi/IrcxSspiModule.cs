using System.Reflection;
using System.Runtime.InteropServices;

namespace Irc.Security.Sspi;

public static class IrcxSspiModule
{
	// Best-practice for native dependency resolution in .NET:
	// hook NativeLibrary import resolution early and load from a known location.
	private static bool _initialized;
	private static readonly object Sync = new();

	public static void Initialize()
	{
		lock (Sync)
		{
			if (_initialized)
				return;

			NativeLibrary.SetDllImportResolver(typeof(IrcxSspiModule).Assembly, Resolve);
			_initialized = true;
		}
	}

	private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		// Our P/Invokes use "ircx_sspi".
		if (!string.Equals(libraryName, "ircx_sspi", StringComparison.OrdinalIgnoreCase))
			return IntPtr.Zero;

		// Determine the platform-specific library filename.
		string fileName;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			fileName = "ircx_sspi.dll";
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			fileName = "libircx_sspi.dylib";
		else
			fileName = "libircx_sspi.so"; // Linux and other Unix-like systems

		// Probe upwards from the app base directory to find the repo root, then load from
		// target/(debug|release) or targets/(debug|release).
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		for (var depth = 0; dir is not null && depth < 12; depth++, dir = dir.Parent)
		{
			var root = dir.FullName;
			foreach (var folder in new[] { "target", "targets" })
			{
				foreach (var config in new[] { "release", "debug" })
				{
					var path = Path.Combine(root, folder, config, fileName);
					if (File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
						return handle;
				}
			}
		}

		// Returning zero lets the runtime continue with default probing rules.
		return IntPtr.Zero;
	}
}
