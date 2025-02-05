using System.Collections;
using System.IO.Enumeration;

namespace Pepper.Convert;

internal class FileEnumerator : IEnumerable<string> {
	public FileEnumerator(string[] args, EnumerationOptions options, params string[] patterns) {
		if (options.MatchType == MatchType.Win32) {
			throw new NotSupportedException();
		}

		Paths = [..args];
		Expressions = [..patterns];

		Options = options;
		IgnoreCase = options.MatchCasing == MatchCasing.CaseInsensitive || (options.MatchCasing == MatchCasing.PlatformDefault && SystemIsCaseInsensitive);
	}

	public FileEnumerator(string[] args, params string[] patterns) : this(args, new EnumerationOptions {
		MatchType = MatchType.Simple,
	}, patterns) { }

	public FileEnumerator(string path, EnumerationOptions options, params string[] patterns) : this([path], options, patterns) { }

	public FileEnumerator(string path, params string[] patterns) : this([path], new EnumerationOptions {
		MatchType = MatchType.Simple,
	}, patterns) { }

	private HashSet<string> Paths { get; }
	private HashSet<string> Expressions { get; }
	private EnumerationOptions Options { get; }
	private bool IgnoreCase { get; }

	private static bool SystemIsCaseInsensitive => OperatingSystem.IsWindows();

	public IEnumerator<string> GetEnumerator() {
		foreach (var path in Paths) {
			var info = new FileInfo(path);

			if (!info.Attributes.HasFlag(FileAttributes.Directory) && info.Exists) {
				if (IsMatch(info.Name) || IsMatch(info.FullName)) {
					yield return path;
				}

				continue;
			}

			var dir = new DirectoryInfo(path);
			if (!dir.Exists) {
				continue;
			}

			foreach (var file in dir.GetFiles("*", Options)) {
				if (IsMatch(file.Name) || IsMatch(file.FullName)) {
					yield return Path.Combine(path, Path.GetRelativePath(path, file.FullName));
				}
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private bool IsMatch(string path) {
		foreach (var expression in Expressions) {
			// thank you microsoft for not making this internal.
			if (FileSystemName.MatchesSimpleExpression(expression.AsSpan(), path, IgnoreCase)) {
				return true;
			}
		}

		return false;
	}
}
