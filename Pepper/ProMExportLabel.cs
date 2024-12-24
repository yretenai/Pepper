using System;
using System.Text;
using Pepper.Structures;

namespace Pepper;

public class ProMExportLabel {
	public static readonly WAVEChunkAtom Atom = "proM";

	public ProMExportLabel(WAVELISTLabel label) {
		FriendlyName = null;

		var text = Encoding.UTF8.GetString(label.Buffer.Span);
		var parts = text.Split("--", 2, StringSplitOptions.TrimEntries);
		var header = parts[0];
		var body = parts[1]; // todo: parse this properly, has some nice timing info

		var index = header.IndexOf("FriendlyName", StringComparison.OrdinalIgnoreCase);
		if (index > -1) {
			index = header.IndexOf('=', index);
			if (index > -1) {
				var name = header[(index + 1)..].Trim();
				if (name.Length > 0) {
					if (name[0] == '"') {
						name = name.Substring(1, name.Length - 2);
					}

					FriendlyName = name;
				}
			}
		}
	}

	public string? FriendlyName { get; }
}
