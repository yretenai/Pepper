namespace Pepper.Structures;

public record struct WAVEChunkFragment {
	public WAVEChunkAtom Id { get; set; }
	public int Size { get; set; }
}
