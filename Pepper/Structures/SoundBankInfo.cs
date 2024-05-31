using System;
using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankInfo : SoundBankAsset {
	public List<SoundBankFile> ReferencedStreamedFiles { get; set; } = [];
	public List<SoundBankFile> IncludedMemoryFiles { get; set; } = [];
	public List<SoundBankFile> ExcludedMemoryFiles { get; set; } = [];
	public List<SoundBankParameter> GameParameters { get; set; } = [];
	public List<SoundBankParameter> IncludedAuxBusses { get; set; } = [];
	public List<SoundBankStateGroup> SwitchGroups { get; set; } = [];
	public List<SoundBankStateGroup> StateGroups { get; set; } = [];
	public List<SoundBankEvent> IncludedEvents { get; set; } = [];
}
