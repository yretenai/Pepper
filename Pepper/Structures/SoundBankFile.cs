namespace Pepper.Structures;

public class SoundBankFile {
	public ulong Id { get; set; }
	public string Language { get; set; } = null!;
	public string ShortName { get; set; } = null!;
	public string Path { get; set; } = null!;
	public long PrefetchSize { get; set; }
	public bool UsingReferenceLanguageAsStandIn { get; set; }
}
