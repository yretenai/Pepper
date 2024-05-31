using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBanksInfo {
	public string Platform { get; set; } = "Unknown";
	public string BasePlatform { get; set; } = "Unknown";
	public long SchemaVersion { get; set; }
	public long SoundbankVersion { get; set; }

	public SoundBankPaths RootPaths { get; set; } = new();

	// public List<object> DialogueEvents { get; set; } = null!;
	public List<SoundBankFile> StreamedFiles { get; set; } = [];

	// public List<object> MediaFilesNotInAnyBank { get; set; } = null!;
	public List<SoundBankInfo> SoundBanks { get; set; } = [];
}
