using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public static class WemHelper {
	static WemHelper() {
		var ogg = IntPtr.Zero;
		var vorbis = IntPtr.Zero;
		try {
			var platformPrefix = Environment.OSVersion.Platform is PlatformID.Win32NT ? string.Empty : "lib";
			var platformSuffix = Environment.OSVersion.Platform switch {
				                     PlatformID.Win32NT => "dll",
				                     PlatformID.MacOSX => "dylib",
				                     _ => "so",
			                     };

			if (NativeLibrary.TryLoad($"{platformPrefix}ogg.{platformSuffix}", out ogg) &&
			    NativeLibrary.TryLoad($"{platformPrefix}vorbis.{platformSuffix}", out vorbis)) {
				CanUseRevorb = true;
			}
		} finally {
			if (ogg != IntPtr.Zero) {
				NativeLibrary.Free(ogg);
			}

			if (vorbis != IntPtr.Zero) {
				NativeLibrary.Free(vorbis);
			}
		}
	}

	public static bool CanUseRevorb { get; set; }

	public static WAVECodec GetCodec(Stream stream) => GetFormatChunk(stream).Codec;

	public static WAVEFormatChunk GetFormatChunk(Stream stream) {
		var pos = stream.Position;
		using var riff = new WwiseRIFFFile(stream, true);
		stream.Position = pos;
		return riff.FormatChunk;
	}

	public static WwiseRIFFFile GetDecoder(Stream stream, bool leaveOpen = false, WemCodecOptions? options = default) => GetDecoder(GetCodec(stream), stream, leaveOpen, options);

	public static WwiseRIFFFile GetDecoder(WAVEFormatChunk chunk, Stream stream, bool leaveOpen = false, WemCodecOptions? options = default) => GetDecoder(chunk.Codec, stream, leaveOpen, options);

	public static WwiseRIFFFile GetDecoder(WAVECodec codec, Stream stream, bool leaveOpen = false, WemCodecOptions? options = default) {
		options ??= WemCodecOptions.Default;
		return codec switch {
			       WAVECodec.WwiseOpus => new WwiseRIFFOpus(stream, options.OpusForceStereo, leaveOpen),
			       WAVECodec.WwiseVorbis => new WwiseRIFFVorbis(stream, options.CodebooksPath, leaveOpen),
			       WAVECodec.WwisePTADPCM => new WwiseRIFFPTADPCM(stream, leaveOpen),
			       _ => new WwiseRIFFFile(stream, leaveOpen),
		       };
	}

	public static WwiseType GetType(Stream stream) {
		var data = 0;
		stream.ReadExactly(MemoryMarshal.AsBytes(new Span<int>(ref data)));
		stream.Position -= 4;

		return data switch {
			       0x46464952 => WwiseType.AudioStream,
			       0x44484B42 => WwiseType.Soundbank,
			       0x4b504b41 => WwiseType.AudioPack,
			       _ => WwiseType.Unknown,
		       };
	}

	private static ulong Hash(string text) => text.Aggregate(0xCBF29CE484222325ul, (current, ch) => (current * 0x100000001B3UL) ^ ch);
	private static uint Hash32(string text) => text.Aggregate(0x811C9DC5u, (current, ch) => (current * 0x1000193u) ^ ch);

	public static Dictionary<ulong, string> BuildSoundbankLookup(IEnumerable<string> strings, params string[] languages) {
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
