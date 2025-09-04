#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 
/// </summary>
public record ManifestItem
{
    /// <summary>
    /// 
    /// </summary>
    public string? StringValue { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<KeyValuePair<string, ManifestItem>>? ItemValue { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public ManifestItem[]? ArrayValue { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <returns></returns>
    public string FormatJSON() => JsonPrettyPrinter.Print(ToString() ?? "{}");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public static implicit operator ManifestItem(string value) => new() { StringValue = value };
}
