using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public sealed record WwiseSoundbank : IDisposable, IChunkedFile {
    public WwiseSoundbank(Stream stream) {
        BaseStream = stream;

        WAVEChunkFragment fragment = default;
        var fragmentSpan = new Span<WAVEChunkFragment>(ref fragment);
        var headerOffset = 0;
        var dataIndexOffset = -1;
        while (stream.Position < stream.Length) {
            stream.ReadExactly(MemoryMarshal.AsBytes(fragmentSpan));
            Chunks.Add(stream.Position, fragment);

            switch (fragment.Id) {
                case AKBKHeader.Magic:
                    headerOffset = (int) stream.Position;
                    break;
                case AKBKDataIndex.Magic:
                    dataIndexOffset = (int) stream.Position;
                    break;
                case AKBKData.Magic:
                    DataOffset = stream.Position;
                    break;
            }

            stream.Position += fragment.Size;
        }

        stream.Position = headerOffset;
        AKBKHeader header = default;
        var headerSpan = new Span<AKBKHeader>(ref header);
        stream.ReadExactly(MemoryMarshal.AsBytes(headerSpan));
        Header = header;

        if (dataIndexOffset > -1) {
            stream.Position = dataIndexOffset;
            var info = Chunks[dataIndexOffset];
            var buffer = new byte[info.Size];
            stream.ReadExactly(buffer);
            var index = MemoryMarshal.Cast<byte, AKBKDataIndex>(buffer);
            foreach (var item in index) {
                DataIndex.Add(item.Id, item);
            }
        }
    }

    public Dictionary<uint, AKBKDataIndex> DataIndex { get; } = new();
    public long DataOffset { get; }
    public AKBKHeader Header { get; }
    public Stream BaseStream { get; }

    public Dictionary<long, WAVEChunkFragment> Chunks { get; set; } = new();

    public void Dispose() {
        BaseStream.Dispose();
    }

    public byte[] GetSound(uint id) {
        if (!DataIndex.TryGetValue(id, out var index)) {
            throw new KeyNotFoundException();
        }

        var buffer = new byte[index.Size];
        BaseStream.Position = DataOffset + index.Offset;
        BaseStream.ReadExactly(buffer.AsSpan());
        return buffer;
    }

    public IMemoryOwner<byte> RentSound(uint id, out int size) {
        if (!DataIndex.TryGetValue(id, out var index)) {
            throw new KeyNotFoundException();
        }

        var buffer = MemoryPool<byte>.Shared.Rent(index.Size);
        BaseStream.Position = DataOffset + index.Offset;
        BaseStream.ReadExactly(buffer.Memory.Span[..index.Size]);
        size = index.Size;
        return buffer;
    }
}
