using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankEvent {
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ObjectPath { get; set; } = null!;
    public List<SoundBankParameter> ActionSetState { get; set; } = null!;
    public string DefaultSwitchValue { get; set; } = null!;
    public List<SoundBankSwitch> SwitchContainers { get; set; } = null!;
    public List<SoundBankParameter> ActionSetSwitch { get; set; } = null!;
    public List<SoundBankParameter> ActionPostEvent { get; set; } = null!;
}
