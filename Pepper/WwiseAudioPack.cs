using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Pepper.Structures;

namespace Pepper;

public sealed record WwiseAudioPack : IDisposable {
    public WwiseAudioPack(Stream stream) {
        BaseStream = stream;

        AKPKHeader header = default;
        var headerSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1));
        stream.ReadExactly(headerSpan);
        Header = header;

        var startOfData = stream.Position;
        var endOfTable = stream.Position + header.NameTableSize;
        var count = 0;
        var countSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref count, 1));
        stream.ReadExactly(countSpan);
        var tmp = stream.Position;
        stream.Position = endOfTable - 2;
        var isWide = stream.ReadByte() == 0;
        stream.Position = tmp;

        var names = new Dictionary<int, string>();
        var nameTable = new AKPKName[count];
        var nameTableSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref nameTable[0], nameTable.Length));
        stream.ReadExactly(nameTableSpan);
        foreach (var name in nameTable) {
            stream.Position = startOfData + name.Offset;
            var nameBuffer = new byte[isWide ? 2 : 1];
            var nameBuilder = new StringBuilder();
            while (true) {
                stream.ReadExactly(nameBuffer);
                if (isWide) {
                    if (nameBuffer[0] == 0 && nameBuffer[1] == 0) {
                        break;
                    }

                    nameBuilder.Append((char) (nameBuffer[0] | (nameBuffer[1] << 8)));
                } else {
                    if (nameBuffer[0] == 0) {
                        break;
                    }

                    nameBuilder.Append((char) nameBuffer[0]);
                }
            }

            names.Add(name.Id, nameBuilder.ToString());
        }

        NameTable = names;

        stream.Position = endOfTable + 4;
        var soundbanksCount = (header.BankTableSize - 4) / Unsafe.SizeOf<AKPKEntry>();
        var soundbanks = new AKPKEntry[soundbanksCount];
        if (soundbanksCount > 0) {
            var soundbanksSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref soundbanks[0], soundbanks.Length));
            stream.ReadExactly(soundbanksSpan);
        }

        Soundbanks = soundbanks;

        stream.Position += 4;
        var streamsCount = (header.StreamTableSize - 4) / Unsafe.SizeOf<AKPKEntry>();
        var streams = new AKPKEntry[streamsCount];
        if (streamsCount > 0) {
            var streamsSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref streams[0], streams.Length));
            stream.ReadExactly(streamsSpan);
        }

        Streams = streams;

        stream.Position += 4;
        var externalCount = (header.ExternalTableSize - 4) / Unsafe.SizeOf<AKPKEntry64>();
        var external = new AKPKEntry64[externalCount];
        if (externalCount > 0) {
            var externalSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref external[0], external.Length));
            stream.ReadExactly(externalSpan);
        }

        External = external;
    }

    public AKPKHeader Header { get; init; }
    public Dictionary<int, string> NameTable { get; init; }
    public AKPKEntry[] Soundbanks { get; init; }
    public AKPKEntry[] Streams { get; init; }
    public AKPKEntry64[] External { get; init; }
    public Stream BaseStream { get; init; }
    public string? Tag { get; set; }

    public void Dispose() {
        BaseStream.Dispose();
    }

    public byte[] GetEntryData(IAPKPEntry entry) {
        var owned = new byte[entry.Size];
        BaseStream.Position = entry.Offset;
        BaseStream.ReadExactly(owned.AsSpan());
        return owned;
    }

    public IMemoryOwner<byte> RentSound(IAPKPEntry entry, out int size) {
        BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
        var owned = MemoryPool<byte>.Shared.Rent(entry.Size);
        BaseStream.ReadExactly(owned.Memory.Span[..entry.Size]);
        size = entry.Size;
        return owned;
    }
}
