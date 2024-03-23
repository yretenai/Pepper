using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 20)]
public record struct AKPKEntry : IAPKPEntry {
    public uint Id32 { get; set; }
    public uint Alignment { get; set; }
    public int Size { get; set; }
    public int Offset { get; set; }
    public int Folder { get; set; }

    public ulong Id {
        get => Id32;
        set => Id32 = (uint) value;
    }
}
