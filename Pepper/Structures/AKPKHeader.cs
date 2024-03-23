using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 28)]
public record struct AKPKHeader {
    public uint Magic { get; set; }
    public uint Size { get; set; }
    public uint Version { get; set; }
    public int NameTableSize { get; set; }
    public int BankTableSize { get; set; }
    public int StreamTableSize { get; set; }
    public int ExternalTableSize { get; set; }
}
