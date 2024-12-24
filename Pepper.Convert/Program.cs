using Pepper;
using Pepper.Structures;

foreach (var file in args) {
	if (!File.Exists(file)) {
		continue;
	}

	using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
	var type = WemHelper.GetType(fileStream);
	switch (type) {
		case WwiseType.AudioStream: {
			using var codec = WemHelper.GetDecoder(fileStream);
			if (codec.Format == AudioFormat.Wem) {
				continue;
			}

			Console.WriteLine(file);

			using var output = new FileStream(Path.ChangeExtension(file, codec.Format.ToString("G").ToLower()), FileMode.Create, FileAccess.ReadWrite);
			codec.Decode(output);
			break;
		}
		case WwiseType.Soundbank: break; // todo
		case WwiseType.AudioPack: {
			using var pack = new WwiseAudioPack(fileStream);
			if (pack.IsEmpty) {
				continue;
			}

			Console.WriteLine(file);
			var rootDirectory = Path.ChangeExtension(file, null);

			var bnkDirectory = Path.Combine(rootDirectory, "banks");
			var streamDirectory = Path.Combine(rootDirectory, "streams");
			var externalDirectory = Path.Combine(rootDirectory, "external");

			foreach (var ((baseFolder, extension), entries) in new[] { (bnkDirectory, AudioFormat.Bnk), (streamDirectory, AudioFormat.Wem), (externalDirectory, AudioFormat.Wem) }
				        .Zip([pack.Soundbanks.Cast<IAPKPEntry>(), pack.Streams.Cast<IAPKPEntry>(), pack.External.Cast<IAPKPEntry>()])) {
				foreach (var entry in entries) {
					using var rented = pack.RentSound(entry, out var size);
					var name = entry.Id.ToString("D");
					if (extension == AudioFormat.Wem) {
						unsafe {
							using var pin = rented.Memory.Pin();
							using var unmanagedStream = new UnmanagedMemoryStream((byte*) pin.Pointer, size);
							using var riff = new WAVERIFFFile(unmanagedStream);
							if (riff.TryFindNameLabel(out var label)) {
								name += $"_{label}";
							}
						}
					}

					var targetFolder = Path.Combine(baseFolder, pack.GetFolder(entry));
					Directory.CreateDirectory(targetFolder);
					var filename = $"{name}.{extension.ToString("G").ToLower()}";
					Console.WriteLine(filename);
					var target = Path.Combine(targetFolder, filename);
					// todo: convert wem and soundbank?
					using var output = new FileStream(target, FileMode.Create, FileAccess.ReadWrite);
					output.Write(rented.Memory.Span[..size]);
				}
			}

			break;
		}
	}
}
