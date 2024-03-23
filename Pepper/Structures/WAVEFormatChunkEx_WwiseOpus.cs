using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct WAVEFormatChunkEx_WwiseOpus {
    public uint ChannelLayout { get; set; }
    public uint Samples { get; set; }
    public int TableCount { get; set; }
    public ushort Skip { get; set; }
    public byte Version { get; set; }
    public byte MappingFamily { get; set; }
}
