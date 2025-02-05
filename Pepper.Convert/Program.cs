using System.Globalization;
using Pepper.Structures;

namespace Pepper.Convert;

internal static class Program {
	public static void Main(string[] args) {
		if (args.Length == 0) {
			Console.WriteLine("Usage: Pepper.Convert path/to/file");
			Console.WriteLine("Usage: Pepper.Convert path/to/output path/to/files");
			return;
		}

		var output = default(string?);
		if (args.Length > 1 && (!File.Exists(args[0]) || Directory.Exists(args[0]))) {
			output = args[0];
			args = args[1..];
		}

		var files = new FileEnumerator(args, new EnumerationOptions {
			RecurseSubdirectories = true,
		}, "*.*").ToList();

		var paths = BuildPathMap(files);

		foreach (var file in files) {
			if (!File.Exists(file)) {
				continue;
			}

			using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var type = WemHelper.GetType(fileStream);
			switch (type) {
				case WwiseType.AudioStream: {
					try {
						HandleWem(paths, output, fileStream, Path.GetFileNameWithoutExtension(file), file);
					} catch (Exception e) {
						Console.Error.WriteLine($"Failed converting wem stream {file}: {e}");
					}

					continue;
				}
				case WwiseType.Soundbank: {
					HandleBank(paths, output, fileStream, file);
					continue;
				}
				case WwiseType.AudioPack: {
					using var pack = new WwiseAudioPack(fileStream);
					if (pack.IsEmpty) {
						continue;
					}

					var rootDirectory = Path.ChangeExtension(file, null);
					var bnkDirectory = Path.Combine(rootDirectory, "banks");
					var streamDirectory = Path.Combine(rootDirectory, "streams");
					var externalDirectory = Path.Combine(rootDirectory, "external");

					foreach (var ((baseFolder, extension), entries) in new[] { (bnkDirectory, AudioFormat.Bnk), (streamDirectory, AudioFormat.Wem), (externalDirectory, AudioFormat.Wem) }
						        .Zip([pack.Soundbanks.Cast<IAPKPEntry>(), pack.Streams.Cast<IAPKPEntry>(), pack.External.Cast<IAPKPEntry>()])) {
						foreach (var entry in entries) {
							using var rented = pack.RentSound(entry, out var size);
							var name = entry.Id.ToString("D");
							var targetFolder = Path.Combine(baseFolder, pack.GetFolder(entry));
							Directory.CreateDirectory(targetFolder);
							var filename = $"{name}.{extension.ToString("G").ToLower()}";
							Console.WriteLine(filename);
							var target = Path.Combine(targetFolder, filename);
							// todo: convert wem and soundbank?
							using var outputStream = new FileStream(target, FileMode.Create, FileAccess.ReadWrite);
							outputStream.Write(rented.Memory.Span[..size]);
						}
					}

					break;
				}
			}
		}
	}

	private static void HandleBank(Dictionary<long, string> paths, string? output, Stream stream, string file) {
		Console.WriteLine(file);

		// todo: convert handle HIRC to determine filename if no paths are present

		using var bnk = new WwiseSoundbank(stream);
		foreach (var id in bnk.DataIndex.Keys) {
			using var rented = bnk.RentSound(id, out var size);
			unsafe {
				using var pin = rented.Memory.Pin();
				using var unmanagedStream = new UnmanagedMemoryStream((byte*) pin.Pointer, size);
				try {
					HandleWem(paths, output, unmanagedStream, id.ToString("D"), file);
				} catch (Exception e) {
					Console.Error.WriteLine($"Failed converting wem stream {id} in bank {file}: {e}");
				}
			}
		}
	}

	private static void HandleWem(Dictionary<long, string> paths, string? output, Stream stream, string name, string file) {
		using var codec = WemHelper.GetDecoder(stream);
		if (codec.Format == AudioFormat.Wem) {
			return;
		}

		if (long.TryParse(Path.GetFileNameWithoutExtension(name), NumberStyles.Integer, null, out var id)) {
			if (paths.TryGetValue(id, out var path)) {
				if (output == null) {
					name += $"_{Path.ChangeExtension(path, null)}";
				} else {
					name = Path.ChangeExtension(path, null);
				}
			} else if (codec.TryFindNameLabel(out path)) {
				if (output == null) {
					name += $"_{path.Unix().Replace('/', '_')}";
				} else {
					name = path;
				}
			}
		}

		name = name.Unix().Trim('/', '.', '~', '$');

		Console.WriteLine(name);

		output ??= Path.GetDirectoryName(file)!;
		var outputPath = Path.Combine(output, name) + "." + codec.Format.ToString("G").ToLower();
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite);
		codec.Decode(outputStream);
	}

	private static Dictionary<long, string> BuildPathMap(List<string> files) {
		var infos = files.Where(x => Path.GetFileNameWithoutExtension(x).Equals("soundbanksinfo", StringComparison.OrdinalIgnoreCase)).Select(x => new WwiseSoundbanksInfo(x)).ToArray();
		var streams = infos.SelectMany(x => x.SoundBanksInfo.StreamedFiles).DistinctBy(x => x.Id).ToDictionary(x => x.Id, x => x);
		var result = new Dictionary<long, string>();

		foreach (var bank in infos.SelectMany(x => x.SoundBanksInfo.SoundBanks).DistinctBy(x => x.Path)) {
			var bankPath = bank.ObjectPath.Unix();

			foreach (var memoryFile in bank.IncludedMemoryFiles) {
				BuildPath(result, memoryFile.Id, memoryFile.Language, bankPath, memoryFile.ShortName.Unix());
			}

			foreach (var streamFile in bank.ReferencedStreamedFiles) {
				if (!streams.TryGetValue(streamFile.Id, out var memoryFile)) {
					continue;
				}

				BuildPath(result, streamFile.Id, memoryFile.Language, bankPath, memoryFile.ShortName.Unix());
			}
		}

		foreach (var memoryFile in streams.Values) {
			BuildPath(result, memoryFile.Id, memoryFile.Language, string.Empty, memoryFile.ShortName.Unix());
		}

		return result;
	}

	private static void BuildPath(Dictionary<long, string> result, long id, string language, string basePath, string filePath) {
		if (result.ContainsKey(id)) {
			return;
		}

		basePath = basePath.TrimEnd('/');
		filePath = filePath.TrimEnd('/');

		if (!language.Equals("sfx", StringComparison.OrdinalIgnoreCase)) {
			basePath += $"/{language}";
		}

		basePath = basePath.Trim('/');

		var combined = $"{basePath}/{filePath}";
		result[id] = combined;
	}
}
