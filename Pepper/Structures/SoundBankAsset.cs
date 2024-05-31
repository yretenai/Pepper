namespace Pepper.Structures;

public class SoundBankAsset {
	public long Id { get; set; }
	public string Language { get; set; } = string.Empty;
	public long Hash { get; set; }
	public string ObjectPath { get; set; } = string.Empty;
	public string ShortName { get; set; } = string.Empty;
	public string Path { get; set; } = string.Empty;
	public string GUID { get; set; } = string.Empty;

	public virtual string Name {
		get => ShortName;
		set => ShortName = value;
	}
}
