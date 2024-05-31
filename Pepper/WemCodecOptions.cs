namespace Pepper;

public record WemCodecOptions {
	public static WemCodecOptions Default { get; } = new();
	public string CodebooksPath { get; init; } = "packed_codebooks_aoTuV_603.bin";
	public bool OpusForceStereo { get; init; }
}
