using System.Collections.Generic;

namespace Pepper.Structures;

public class SoundBanksInfo {
    public string Platform { get; set; } = null!;
    public string BasePlatform { get; set; } = null!;
    public long SchemaVersion { get; set; }
    public long SoundbankVersion { get; set; }

    public SoundBankPaths SoundBanksInfoRootPaths { get; set; } = null!;

    // public List<object> DialogueEvents { get; set; } = null!;
    public List<SoundBankFile> StreamedFiles { get; set; } = null!;

    // public List<object> MediaFilesNotInAnyBank { get; set; } = null!;
    public List<SoundBankInfo> SoundBanks { get; set; } = null!;
}
