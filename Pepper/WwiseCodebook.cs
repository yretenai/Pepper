using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public class WwiseCodebook {
    public WwiseCodebook(Stream stream) {
        stream.Seek(-4, SeekOrigin.End);
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        Count = BinaryPrimitives.ReadUInt32LittleEndian(buffer);

        Data = new byte[Count];
        Offsets = new int[(stream.Length - Count - 4) >> 2];

        stream.Position = 0;
        stream.ReadExactly(Data.Span);
        stream.ReadExactly(MemoryMarshal.AsBytes(Offsets.Span));
    }

    public Memory<byte> Data { get; }
    public Memory<int> Offsets { get; }
    public uint Count { get; }

    public Memory<byte> GetCodebook(int i) => i >= Count - 1 || i < 0 ? Memory<byte>.Empty : Data[Offsets.Span[i]..];

    internal void Rebuild(int codebookID, BitOggStream bos) {
        var bis = new BitStream(GetCodebook(codebookID));

        var dimensions = bis.Read(4);
        var entries = bis.Read(14);

        bos.Write(new BitUint(24, 0x564342));
        bos.Write(new BitUint(16, dimensions));
        bos.Write(new BitUint(24, entries));

        var ordered = bis.Read(1);
        bos.Write(ordered);

        if (ordered == 1) {
            var initialLength = bis.Read(5);
            bos.Write(initialLength);

            var currentEntry = 0;
            while (currentEntry < entries) {
                var number = bis.Read((byte) (32 - BitOperations.LeadingZeroCount((uint) (entries - currentEntry))));
                bos.Write(number);
                currentEntry = (int) (currentEntry + number);
            }

            if (currentEntry > entries) {
                throw new Exception("current_entry out of range");
            }
        } else {
            var codewordLengthLength = bis.Read(3);
            var sparse = bis.Read(1);

            if (0 == codewordLengthLength || 5 < codewordLengthLength) {
                throw new Exception("nonsense codeword length");
            }

            bos.Write(sparse);

            for (var i = 0; i < entries; i++) {
                var presentBool = true;

                if (sparse == 1) {
                    var present = bis.Read(1);
                    bos.Write(present);
                    presentBool = 0 != present;
                }

                if (presentBool) {
                    var codewordLength = bis.Read((byte) codewordLengthLength);
                    bos.Write(new BitUint(5, codewordLength));
                }
            }
        }

        var lookupType = bis.Read(1);
        bos.Write(new BitUint(4, lookupType));

        if (lookupType == 1) {
            var min = bis.Read(32);
            var max = bis.Read(32);
            var valueLength = bis.Read(4);
            var sequenceFlag = bis.Read(1);

            bos.Write(min);
            bos.Write(max);
            bos.Write(valueLength);
            bos.Write(sequenceFlag);

            var quantvals = Q(entries, dimensions);
            for (uint i = 0; i < quantvals; i++) {
                var val = bis.Read((byte) (valueLength + 1));
                bos.Write(val);
            }
        }
    }

    private static uint Q(uint entries, uint dimensions) {
        var bits = (byte) (32 - BitOperations.LeadingZeroCount(entries));
        var vals = (int) (entries >> (int) ((bits - 1) * (dimensions - 1) / dimensions));
        while (true) {
            uint acc = 1;
            uint acc1 = 1;
            uint i;
            for (i = 0; i < dimensions; i++) {
                acc = (uint) (acc * vals);
                acc1 = (uint) (acc * vals + 1);
            }

            if (acc <= entries && acc1 > entries) {
                return (uint) vals;
            }

            if (acc > entries) {
                vals--;
            } else {
                vals++;
            }
        }
    }
}
