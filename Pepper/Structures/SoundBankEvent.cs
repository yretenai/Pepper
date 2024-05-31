using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankEvent : SoundBankParameter {
	public List<SoundBankParameter> ActionSetState { get; set; } = [];
	public string? DefaultSwitchValue { get; set; }
	public List<SoundBankSwitch> SwitchContainers { get; set; } = [];
	public List<SoundBankParameter> ActionSetSwitch { get; set; } = [];
	public List<SoundBankParameter> ActionPostEvent { get; set; } = [];
}
