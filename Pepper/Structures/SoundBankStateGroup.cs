using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankStateGroup : SoundBankParameter {
	public List<SoundBankStateGroup> States { get; set; } = [];
	public List<SoundBankStateGroup> Switches { get; set; } = [];
}
