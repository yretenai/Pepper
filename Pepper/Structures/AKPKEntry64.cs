using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 24)]
public record struct AKPKEntry64 : IAPKPEntry {
    public ulong Id { get; set; }
    public uint Alignment { get; set; }
    public int Size { get; set; }
    public int Offset { get; set; }
    public int Folder { get; set; }
}
