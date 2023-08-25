using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Devlooped.SponsorLink;

static class JsonOptions
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(allowIntegerValues: false),
            new DateOnlyJsonConverter()
        }
    };

    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateOnly.Parse(reader.GetString()?[..10] ?? "", CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
    }
}
