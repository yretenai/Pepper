using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankSwitch : SoundBankSwitchChild {
	public List<SoundBankSwitchChild> Children { get; set; } = [];
}
