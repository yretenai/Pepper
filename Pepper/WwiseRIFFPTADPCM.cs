using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

// ported from vgmstream
public record WwiseRIFFPTADPCM : WwiseRIFFFile {
    private static readonly int[,,] TABLE = { {
            {   -14,  2},  {   -10,  2},  {    -7,  1},  {    -5,  1},   {   -3,  0},   {   -2,  0},   {   -1,  0},   {    0,  0},
            {     0,  0},  {     1,  0},  {     2,  0},  {     3,  0},   {    5,  1},   {    7,  1},   {   10,  2},   {   14,  2},
        }, {
            {   -28,  3},  {   -20,  3},  {   -14,  2},  {   -10,  2},   {   -7,  1},   {   -5,  1},   {   -3,  1},   {   -1,  0},
            {     1,  0},  {     3,  1},  {     5,  1},  {     7,  1},   {   10,  2},   {   14,  2},   {   20,  3},   {   28,  3},
        }, {
            {   -56,  4},  {   -40,  4},  {   -28,  3},  {   -20,  3},   {  -14,  2},   {  -10,  2},   {   -6,  2},   {   -2,  1},
            {     2,  1},  {     6,  2},  {    10,  2},  {    14,  2},   {   20,  3},   {   28,  3},   {   40,  4},   {   56,  4},
        }, {
            {  -112,  5},  {   -80,  5},  {   -56,  4},  {   -40,  4},   {  -28,  3},   {  -20,  3},   {  -12,  3},   {   -4,  2},
            {     4,  2},  {    12,  3},  {    20,  3},  {    28,  3},   {   40,  4},   {   56,  4},   {   80,  5},   {  112,  5},
        }, {
            {  -224,  6},  {  -160,  6},  {  -112,  5},  {   -80,  5},   {  -56,  4},   {  -40,  4},   {  -24,  4},   {   -8,  3},
            {     8,  3},  {    24,  4},  {    40,  4},  {    56,  4},   {   80,  5},   {  112,  5},   {  160,  6},   {  224,  6},
        }, {
            {  -448,  7},  {  -320,  7},  {  -224,  6},  {  -160,  6},   { -112,  5},   {  -80,  5},   {  -48,  5},   {  -16,  4},
            {    16,  4},  {    48,  5},  {    80,  5},  {   112,  5},   {  160,  6},   {  224,  6},   {  320,  7},   {  448,  7},
        }, {
            {  -896,  8},  {  -640,  8},  {  -448,  7},  {  -320,  7},   { -224,  6},   { -160,  6},   {  -96,  6},   {  -32,  5},
            {    32,  5},  {    96,  6},  {   160,  6},  {   224,  6},   {  320,  7},   {  448,  7},   {  640,  8},   {  896,  8},
        }, {
            { -1792,  9},  { -1280,  9},  {  -896,  8},  {  -640,  8},   { -448,  7},   { -320,  7},   { -192,  7},   {  -64,  6},
            {    64,  6},  {   192,  7},  {   320,  7},  {   448,  7},   {  640,  8},   {  896,  8},   { 1280,  9},   { 1792,  9},
        }, {
            { -3584, 10},  { -2560, 10},  { -1792,  9},  { -1280,  9},   { -896,  8},   { -640,  8},   { -384,  8},   { -128,  7},
            {   128,  7},  {   384,  8},  {   640,  8},  {   896,  8},   { 1280,  9},   { 1792,  9},   { 2560, 10},   { 3584, 10},
        }, {
            { -7168, 11},  { -5120, 11},  { -3584, 10},  { -2560, 10},   {-1792,  9},   {-1280,  9},   { -768,  9},   { -256,  8},
            {   256,  8},  {   768,  9},  {  1280,  9},  {  1792,  9},   { 2560, 10},   { 3584, 10},   { 5120, 11},   { 7168, 11},
        }, {
            {-14336, 11},  {-10240, 11},  { -7168, 11},  { -5120, 11},   {-3584, 10},   {-2560, 10},   {-1536, 10},   { -512,  9},
            {   512,  9},  {  1536, 10},  {  2560, 10},  {  3584, 10},   { 5120, 11},   { 7168, 11},   {10240, 11},   {14336, 11},
        },  {
            {-28672, 11},  {-20480, 11},  {-14336, 11},  {-10240, 11},   {-7168, 11},   {-5120, 11},   {-3072, 11},   {-1024, 10},
            {  1024, 10},  {  3072, 11},  {  5120, 11},  {  7168, 11},   {10240, 11},   {14336, 11},   {20480, 11},   {28672, 11},
        },
    };

	public WwiseRIFFPTADPCM(Stream stream, bool leaveOpen = false) : base(stream, leaveOpen) {
		if (FormatChunk.Codec is not WAVECodec.WwisePTADPCM) {
			throw new InvalidDataException("Not a Wwise PTADPCM file");
		}

		var dataChunk = Chunks[DataOffset];
		InterleavedFrameSize = FormatChunk.BlockSize / FormatChunk.Channels;
		SamplesPerFrame = 2 + (InterleavedFrameSize - 5) * 2;
		NumSamples = dataChunk.Size / (FormatChunk.Channels * InterleavedFrameSize) * SamplesPerFrame;
	}

	public override AudioFormat Format => AudioFormat.Wav;

	public long NumSamples { get; }
	public int SamplesPerFrame { get; }
	public int InterleavedFrameSize { get; }

	public override void Decode(Stream outputStream) {
		var channels = new short[FormatChunk.Channels][];
		var sampleOffset = new int[FormatChunk.Channels];

		for (var i = 0; i < channels.Length; i++) {
			channels[i] = new short[NumSamples];
		}

		var dataChunk = Chunks[DataOffset];
		using var buffer = MemoryPool<byte>.Shared.Rent((int) dataChunk.Size);
		Stream.Position = DataOffset;
		Stream.ReadExactly(buffer.Memory.Span[..(int) dataChunk.Size]);

		var offset = 0;

		for (var i = 0; i < NumSamples; i += SamplesPerFrame) {
			for (var ch = 0; ch < channels.Length; ++ch) {
				var frame = buffer.Memory.Span.Slice(offset, InterleavedFrameSize);
				offset += InterleavedFrameSize;

				var hist2 = BinaryPrimitives.ReadInt16LittleEndian(frame);
				var hist1 = BinaryPrimitives.ReadInt16LittleEndian(frame[2..]);
				var stepIndex = (int) frame[4];

				if (stepIndex > 12) {
					stepIndex = 12;
				}

				channels[ch][sampleOffset[ch]++] = hist2;
				channels[ch][sampleOffset[ch]++] = hist1;

				for (var sampleIndex = 0; sampleIndex < SamplesPerFrame - 2; ++sampleIndex) {
					var nibbles = frame[5 + sampleIndex / 2];
					var nibble = (sampleIndex & 1) == 0 ? (nibbles >> 0) & 0xF : (nibbles >> 4) & 0xF;

					var step = TABLE[stepIndex, nibble, 0];
					stepIndex = TABLE[stepIndex, nibble, 1];
					var sample = step + 2 * hist1 - hist2;
					sample = sample switch {
						         > 32767 => 32767,
						         < -32768 => -32768,
						         _ => sample,
					         };

					channels[ch][sampleOffset[ch]++] = (short) sample;

					hist2 = hist1;
					hist1 = (short) sample;
				}
			}
		}

		var dataLength = NumSamples * FormatChunk.Channels * sizeof(short);
		var totalWavLength = 44 + dataLength;

		Span<byte> header = stackalloc byte[44];
		var header16 = MemoryMarshal.Cast<byte, ushort>(header);
		var header32 = MemoryMarshal.Cast<byte, uint>(header);
		header32[0] = 0x46464952; // "RIFF"
		header32[1] = (uint) (totalWavLength - 8);
		header32[2] = 0x45564157; // "WAVE"
		header32[3] = 0x20746D66; // "fmt "
		header32[4] = 16;
		header32[5] = 0x11; // WAVE_FORMAT_EXTENSIBLE
		header16[10] = 0x1; // WAVE_FORMAT_PCM
		header16[11] = FormatChunk.Channels;
		header32[6] = (uint) FormatChunk.SampleRate;
		header32[7] = (uint) (FormatChunk.SampleRate * FormatChunk.Channels * sizeof(short)); // bytes per second
		header16[16] = (ushort) (FormatChunk.Channels * sizeof(short)); // block align
		header16[17] = 16; // bits per sample
		header32[9] = 0x61746164; // "data"
		header32[10] = (uint) dataLength;

		outputStream.Write(header);
		for (var i = 0; i < NumSamples; i++) {
			foreach (var sample in channels) {
				outputStream.WriteByte((byte) (sample[i] & 0xFF));
				outputStream.WriteByte((byte) (sample[i] >> 8));
			}
		}
	}
}
