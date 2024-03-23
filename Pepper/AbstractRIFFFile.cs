using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public abstract record AbstractRIFFFile : IDisposable, IChunkedFile {
	public string Vendor { get; set; } = "Pepper";

    protected AbstractRIFFFile(Stream stream, bool leaveOpen = false) {
        Stream = stream;
        LeaveOpen = leaveOpen;

        FileStart = stream.Position;
        Span<uint> header = stackalloc uint[3];
        var buffer = MemoryMarshal.AsBytes(header);
        stream.ReadExactly(buffer);
        if (header[0] is not 0x46464952 || header[2] is not 0x45564157) { // "RIFF", "WAVE"
            throw new InvalidDataException("Not a WAVE-RIFF file");
        }

        FileSize = (int) header[1] + 8;

        WAVEChunkFragment fragment = default;
        var fragmentSpan = new Span<WAVEChunkFragment>(ref fragment);
        while (stream.Position < FileStart + FileSize) {
            stream.ReadExactly(MemoryMarshal.AsBytes(fragmentSpan));
            Chunks.Add(stream.Position, fragment);

            switch (fragment.Id) {
                case 0x20746d66:
                    FormatOffset = stream.Position;
                    break;
                case 0x61746164:
                    DataOffset = stream.Position;
                    break;
            }

            stream.Position += fragment.Size;
        }

        var fmtChunk = Chunks[FormatOffset];
        if (fmtChunk.Size < 0x10) {
            throw new InvalidDataException("Invalid fmt size");
        }

        stream.Position = FormatOffset;
        WAVEFormatChunk fmt = default;
        var fmtSpan = new Span<WAVEFormatChunk>(ref fmt);
        stream.ReadExactly(MemoryMarshal.AsBytes(fmtSpan));
        FormatChunk = fmt;
        buffer = stackalloc byte[2];
        if (fmtChunk.Size >= 0x12) {
            stream.ReadExactly(buffer);
            FormatExtraSize = BinaryPrimitives.ReadUInt16LittleEndian(buffer) - 2;
        }
    }

    public virtual AudioFormat Format => AudioFormat.Ogg;
    protected Stream Stream { get; set; }
    protected int FormatExtraSize { get; }
    private bool LeaveOpen { get; }
    public WAVEFormatChunk FormatChunk { get; protected set; }
    public int FileSize { get; }
    public long FileStart { get; }
    public long FormatOffset { get; }
    public long DataOffset { get; }
    public Dictionary<long, WAVEChunkFragment> Chunks { get; set; } = new();

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~AbstractRIFFFile() {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            if (!LeaveOpen) {
                Stream.Dispose();
            }
        }
    }

    public abstract void Decode(Stream outputStream);
}
