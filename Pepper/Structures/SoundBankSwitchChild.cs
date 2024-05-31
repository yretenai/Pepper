using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankSwitchChild {
	public string SwitchValue { get; set; } = "";
	public List<SoundBankAsset> Media { get; set; } = [];
}
