using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
public record struct AKBKDataIndex {
    public const uint Magic = 0x58444944; // DIDX

    public uint Id { get; set; }
    public int Offset { get; set; }
    public int Size { get; set; }
}
