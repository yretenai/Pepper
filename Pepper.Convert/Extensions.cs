namespace Pepper.Convert;

internal static class Extensions {
	public static string Unix(this string path) => path.Replace('\\', '/').TrimStart('/');
}
