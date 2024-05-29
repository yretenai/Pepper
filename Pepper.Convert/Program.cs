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
		case WwiseType.AudioPack: break; // todo
	}
}
