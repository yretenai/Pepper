using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankSwitch {
	public string SwitchValue { get; set; } = null!;

	// public List<object> Media { get; set; } = null!;
	public List<SoundBankChild> Children { get; set; } = null!;
}
