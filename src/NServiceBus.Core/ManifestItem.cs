namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    public record ManifestItem
    {
        public string? StringValue { get; set; }
        public IEnumerable<KeyValuePair<string, ManifestItem>>? ItemValue { get; set; }
        public ManifestItem[]? ArrayValue { get; set; }

        public override string ToString()
        {
            if (StringValue is not null)
            {
                return $"\"{StringValue}\"";
            }
            if (ItemValue is not null)
            {
                return $@"{{ {string.Join(", ", ItemValue.Select(kvp => $"\"{kvp.Key}\": {kvp.Value}"))} }}";
            }
            if (ArrayValue is not null)
            {
                return $@"[ {string.Join(", ", ArrayValue)} ]";
            }

            return base.ToString();
        }

        public string FormatJSON() => JsonSerializer.Serialize(JsonSerializer.Deserialize<dynamic>(ToString()), new JsonSerializerOptions { WriteIndented = true });

        public static implicit operator ManifestItem(string value) => new() { StringValue = value };
    }
}
