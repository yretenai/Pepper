using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Pepper.Structures;

namespace Pepper;

public class WwiseSoundbanksInfo {
    public class BooleanConverter : JsonConverter<bool> {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch {
                JsonTokenType.True  => true,
                JsonTokenType.False => false,
                JsonTokenType.String => reader.GetString() switch {
                                            "true"  => true,
                                            "false" => false,
                                            _       => throw new JsonException()
                                        },
                _ => throw new JsonException()
            };

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) {
            writer.WriteBooleanValue(value);
        }
    }

    public WwiseSoundbanksInfo(string path) {
        using var reader = new StreamReader(path);
        if (Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase)) {
            var serializer = new XmlSerializer(typeof(SoundBanksInfo));
            SoundBanksInfo = (SoundBanksInfo) serializer.Deserialize(reader)!;
        } else {
            SoundBanksInfo = JsonSerializer.Deserialize<SoundBanksInfoRoot>(reader.ReadToEnd(), new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString, Converters = { new BooleanConverter() } })!.SoundBanksInfo;
        }
    }

    public SoundBanksInfo SoundBanksInfo { get; set; }
}
