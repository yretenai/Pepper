using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
public record struct AKPKName {
    public uint Offset { get; set; }
    public int Id { get; set; }
}
