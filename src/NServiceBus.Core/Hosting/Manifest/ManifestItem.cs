#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class for storing manifest information in a structured way.
/// </summary>
public class ManifestItems
{
    /// <summary>
    /// Adds a new section to the diagnostics.
    /// </summary>
    public void Add(string name, ManifestItem manifestItem) => entries.Add(new(name, manifestItem));

    readonly List<KeyValuePair<string, ManifestItem>> entries = [];

    /// <summary>
    /// Returns a formatted JSON representation of the manifest items.
    /// </summary>
    /// <returns>Manifest as JSON.</returns>
    public string FormatJSON() => JsonPrettyPrinter.Print(new ManifestItem { ItemValue = entries }.ToString()!);

    /// <summary>
    /// Object for storing manifest information in a structured way.
    /// </summary>
    public record ManifestItem
    {
        /// <summary>
        /// String value if applicable.
        /// </summary>
        public string? StringValue { get; init; }
        /// <summary>
        /// Item value if applicable.
        /// </summary>
        public IEnumerable<KeyValuePair<string, ManifestItem>>? ItemValue { get; init; }
        /// <summary>
        /// Array value if applicable.
        /// </summary>
        public ManifestItem[]? ArrayValue { get; init; }

        /// <summary>
        /// Returns a string representation of the manifest item.
        /// </summary>
        /// <returns>Manifest item as string.</returns>
        public override string? ToString()
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

        /// <summary>
        /// Convert from string to a StringValue ManifestItem
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ManifestItem(string value) => new() { StringValue = value };
    }
}
