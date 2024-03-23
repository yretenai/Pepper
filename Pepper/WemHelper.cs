using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pepper.Structures;

namespace Pepper;

public static class WemHelper {
    public static WAVECodec GetCodec(Stream stream) {
        var pos = stream.Position;
        using var riff = new DummyRIFFFile(stream, true);
        stream.Position = pos;
        return riff.FormatChunk.Codec;
    }

    public static AbstractRIFFFile GetDecoder(Stream stream, bool leaveOpen = false, string codebooksPath = "packed_codebooks_aoTuV_603.bin", bool opusForceStereo = false) =>
        GetCodec(stream) switch {
            WAVECodec.WwiseOpus    => new WwiseRIFFOpus(stream, opusForceStereo, leaveOpen),
            WAVECodec.WwiseVorbis  => new WwiseRIFFVorbis(stream, codebooksPath, leaveOpen),
            WAVECodec.WwisePTADPCM => new WwiseRIFFPTADPCM(stream, leaveOpen),
            _                      => new WwiseRIFFDummy(stream, leaveOpen),
        };

    private sealed record DummyRIFFFile : AbstractRIFFFile {
        public DummyRIFFFile(Stream stream, bool leaveOpen) : base(stream, leaveOpen) { }
        public override void Decode(Stream outputStream) => throw new NotSupportedException();
    }

    private static ulong Hash(string text) => text.Aggregate(0xCBF29CE484222325ul, (current, ch) => current * 0x100000001B3UL ^ ch);
    private static uint Hash32(string text) => text.Aggregate(0x811C9DC5u, (current, ch) => current * 0x1000193u ^ ch);

    public static Dictionary<ulong,string> BuildSoundbankLookup(IEnumerable<string> strings, params string[] languages) {
        var lut = new Dictionary<ulong, string>();

        foreach (var str in strings) {
            foreach (var lang in languages) {
                var test = (!string.IsNullOrEmpty(lang) ? $"{lang}\\" : "") + str.Replace('/', '\\');
                lut[Hash(test.ToLowerInvariant())] = test;
                lut[Hash32(test.ToLowerInvariant())] = test;
                test = test.Replace('\\', '/');
                lut[Hash(test.ToLowerInvariant())] = test;
                lut[Hash32(test.ToLowerInvariant())] = test;
            }
        }

        return lut;
    }
}
