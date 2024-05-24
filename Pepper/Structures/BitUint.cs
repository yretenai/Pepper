namespace Pepper.Structures;

internal readonly record struct BitUint(uint BitSize, uint Value = 0) {
	public static implicit operator uint(BitUint bitUint) => bitUint.Value;

	public int AsInt() => (int) Value;
}
