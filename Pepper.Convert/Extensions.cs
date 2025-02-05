namespace Pepper.Convert;

internal static class Extensions {
	public static string Unix(this string path) {
		return path.Replace('\\', '/').TrimStart('/');
	}
}
