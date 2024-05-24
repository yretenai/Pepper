using System;
using System.Buffers.Binary;
using System.IO;

namespace Pepper.Structures;

public readonly record struct VorbisPacket {
    public VorbisPacket(Stream stream, long offset, bool noGranule = false) {
        NoGranule = noGranule;
        OffsetBase = offset;
        stream.Seek(OffsetBase, SeekOrigin.Begin);
        Span<byte> stack = stackalloc byte[6];
        stream.ReadExactly(stack[..2]);
        Size = BinaryPrimitives.ReadUInt16LittleEndian(stack);
        if (!NoGranule) {
            stream.ReadExactly(stack[2..]);
            Granule = BinaryPrimitives.ReadUInt32LittleEndian(stack[2..]);
        }
    }

    public long OffsetBase { get; }
    public ushort Size { get; }
    public uint Granule { get; }
    public bool NoGranule { get; }

    public long HeaderSize() => NoGranule ? 2 : 6;

    public long Offset() => OffsetBase + HeaderSize();

    public long NextOffset() => OffsetBase + HeaderSize() + Size;
}
