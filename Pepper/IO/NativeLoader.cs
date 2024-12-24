using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Pepper.IO;

public static class NativeLoader {
	internal static nint DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
		if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle)) {
			return handle;
		}

		var name = Path.GetFileNameWithoutExtension(libraryName);
		var cwd = AppDomain.CurrentDomain.BaseDirectory;

		string ext;
		if (OperatingSystem.IsWindows()) {
			ext = ".dll";
		} else if (OperatingSystem.IsLinux()) {
			ext = ".so";
		} else if (OperatingSystem.IsMacOS()) {
			ext = ".dylib";
		} else {
			return nint.Zero;
		}

		foreach (var dir in new[] { Path.Combine(cwd, $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/"), cwd }) {
			foreach (var libName in new[] { name, "lib" + name, name + "-0", $"lib{name}-0" }) {
				var target = Path.Combine(dir, libName) + ext;
				if (File.Exists(target)) {
					var ptr = NativeLibrary.Load(target);
					if (ptr != nint.Zero) {
						return ptr;
					}
				}
			}
		}

		return nint.Zero;
	}
}
