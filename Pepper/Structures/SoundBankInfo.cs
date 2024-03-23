using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBankInfo {
    public string Id { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public string ObjectPath { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public string Path { get; set; } = null!;
    public List<SoundBankFile> ReferencedStreamedFiles { get; set; } = null!;
    public List<SoundBankFile> IncludedMemoryFiles { get; set; } = null!;
    public List<SoundBankFile> ExcludedMemoryFiles { get; set; } = null!;
    public List<SoundBankParameter> GameParameters { get; set; } = null!;
    public List<SoundBankParameter> IncludedAuxBusses { get; set; } = null!;
    public List<SoundBankStateGroup> SwitchGroups { get; set; } = null!;
    public List<SoundBankStateGroup> StateGroups { get; set; } = null!;
    public List<SoundBankEvent> IncludedEvents { get; set; } = null!;
}
