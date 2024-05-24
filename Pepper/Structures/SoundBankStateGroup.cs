using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankStateGroup {
	public string Id { get; set; } = null!;
	public string Name { get; set; } = null!;
	public string ObjectPath { get; set; } = null!;
	public string Guid { get; set; } = null!;
	public List<SoundBankStateGroup> States { get; set; } = null!;
	public List<SoundBankStateGroup> Switches { get; set; } = null!;
}
