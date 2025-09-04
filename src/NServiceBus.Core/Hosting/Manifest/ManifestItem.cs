#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class for storing manifest information in a structured way.
/// </summary>
public record ManifestItem
{
    /// <summary>
    /// String value if applicable.
    /// </summary>
    public string? StringValue { get; set; }
    /// <summary>
    /// Item value if applicable.
    /// </summary>
    public IEnumerable<KeyValuePair<string, ManifestItem>>? ItemValue { get; set; }
    /// <summary>
    /// Array value if applicable.
    /// </summary>
    public ManifestItem[]? ArrayValue { get; set; }

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
    /// Returns a formatted JSON representation of the manifest item.
    /// </summary>
    /// <returns>Manifest item as JSON.</returns>
    public string FormatJSON() => JsonPrettyPrinter.Print(ToString() ?? "{}");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public static implicit operator ManifestItem(string value) => new() { StringValue = value };
}
