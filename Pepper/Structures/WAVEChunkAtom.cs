using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Pepper.Structures;

[InlineArray(4)] [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1)]
public struct WAVEChunkAtom : IEquatable<WAVEChunkAtom> {
	public static readonly WAVEChunkAtom BankDataAtom = "DATA";
	public static readonly WAVEChunkAtom DataAtom = "data";

	public byte Value;

	public override string ToString() => Encoding.ASCII.GetString(this).Replace((char) 0, ' ');

	public static implicit operator WAVEChunkAtom(uint value) => Unsafe.As<uint, WAVEChunkAtom>(ref value);

	public static implicit operator WAVEChunkAtom(string value) {
		Span<byte> bytes = stackalloc byte[Encoding.ASCII.GetByteCount(value)];
		var n = Encoding.ASCII.GetBytes(value, bytes);

		if (n < Unsafe.SizeOf<WAVEChunkAtom>()) {
			Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<WAVEChunkAtom>()];
			bytes.CopyTo(buffer);
			bytes = buffer;
		}

		return MemoryMarshal.Read<WAVEChunkAtom>(bytes);
	}

	public override bool Equals(object? obj) {
		return obj switch {
			       uint @uint => this == @uint,
			       string @string => this == @string,
			       WAVEChunkAtom chunkAtom => this == chunkAtom,
			       _ => false,
		       };
	}

	public override int GetHashCode() => MemoryMarshal.Read<int>(this);
	public static bool operator ==(WAVEChunkAtom left, WAVEChunkAtom right) => MemoryMarshal.Read<int>(left).Equals(MemoryMarshal.Read<int>(right));
	public static bool operator !=(WAVEChunkAtom left, WAVEChunkAtom right) => !(left == right);
	public bool Equals(WAVEChunkAtom other) => MemoryMarshal.Read<int>(this).Equals(MemoryMarshal.Read<int>(other));
}
