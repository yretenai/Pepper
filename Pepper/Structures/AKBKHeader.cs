using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
public record struct AKBKHeader {
    public const uint Magic = 0x44484B42; // BKHD

    public uint Version { get; set; }
    public uint Id { get; set; }
    public uint LanguageId { get; set; }
}
