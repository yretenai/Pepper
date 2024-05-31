using System;

namespace Pepper.Structures;

public class SoundBankFile : SoundBankAsset {
	public long PrefetchSize { get; set; }
	public bool UsingReferenceLanguageAsStandIn { get; set; }
}
