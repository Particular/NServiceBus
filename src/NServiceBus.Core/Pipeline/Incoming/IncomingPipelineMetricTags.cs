#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Captures possible metric tags that can be applied to a metric throughout the incoming processing pipeline.
/// </summary>
public sealed class IncomingPipelineMetricTags
{
    readonly Dictionary<string, KeyValuePair<string, object?>> tags = [];
    readonly Dictionary<string, Dictionary<string, KeyValuePair<string, object?>>> instrumentTags = [];

    /// <summary>
    /// Adds the specified tag and value, scoped to <paramref name="instrumentName"/> only, overwriting any value
    /// previously added for that instrument and tag key. Use this instead of <see cref="Add(string,object)"/> when
    /// the tag is only meaningful for one instrument and shouldn't be visible to other instruments applying tags
    /// from this collection.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Add(string,object)"/>, this overwrites rather than keeping the first value. Per-instrument
    /// tags frequently carry a value that's only valid for the call currently recording that instrument (for
    /// example, which handler just ran) rather than a fact that holds for the whole message, so a later call for
    /// the same instrument and tag key must be able to replace an earlier one.
    /// </remarks>
    /// <param name="tagKey">The tag to add.</param>
    /// <param name="value">The value assigned to the tag.</param>
    /// <param name="instrumentName">The instrument the tag applies to.</param>
    public void Add(string tagKey, object value, string instrumentName)
    {
        if (!instrumentTags.TryGetValue(instrumentName, out var perInstrumentTags))
        {
            perInstrumentTags = [];
            instrumentTags.Add(instrumentName, perInstrumentTags);
        }

        perInstrumentTags[tagKey] = new(tagKey, value);
    }

    /// <summary>
    /// Adds the specified tag and value to the collection if not already present.
    /// </summary>
    /// <param name="tagKey">The tag to add.</param>
    /// <param name="value">The value assigned to the tag.</param>
    public void Add(string tagKey, object value)
    {
        // We are using tryAdd to mitigate multiple logical messages transmitted in a single physical message
        tags.TryAdd(tagKey, new(tagKey, value));
    }

    /// <summary>
    /// Applies the specified tag to the <paramref name="tagList"/>. When <paramref name="instrumentName"/> is
    /// provided and a tag was added for that instrument via <see cref="Add(string,object,string)"/>, that value is
    /// used instead of the general one added via <see cref="Add(string,object)"/>.
    /// </summary>
    /// <param name="tagList">The tagList to apply the specified tag to.</param>
    /// <param name="tagKey">The tag to add to the <paramref name="tagList"/>.</param>
    /// <param name="instrumentName">The instrument to prefer instrument-specific tags for, if any.</param>
    public void ApplyTag(ref TagList tagList, string tagKey, string? instrumentName = null)
    {
        if (TryGetTag(tagKey, instrumentName, out var keyValuePair))
        {
            tagList.Add(keyValuePair);
        }
    }

    /// <summary>
    /// Applies the specified tags to the <paramref name="tagList"/>. When <paramref name="instrumentName"/> is
    /// provided and a tag was added for that instrument via <see cref="Add(string,object,string)"/>, that value is
    /// used instead of the general one added via <see cref="Add(string,object)"/>. Any tag added for that
    /// instrument whose key isn't in <paramref name="tagKeys"/> is applied as well: naming an instrument when
    /// adding a tag is already an explicit statement of intent for that one instrument, so callers don't also need
    /// to know about it to pull it in.
    /// </summary>
    /// <param name="tagList">The tagList to add the tags to.</param>
    /// <param name="tagKeys">The collection of tag keys to apply to the <paramref name="tagList"/>.</param>
    /// <param name="instrumentName">The instrument to apply instrument-specific tags for, if any.</param>
    public void ApplyTags(ref TagList tagList, ReadOnlySpan<string> tagKeys, string? instrumentName = null)
    {
        foreach (var tagKey in tagKeys)
        {
            if (TryGetTag(tagKey, instrumentName, out var keyValuePair))
            {
                tagList.Add(keyValuePair);
            }
        }

        if (instrumentName != null && instrumentTags.TryGetValue(instrumentName, out var perInstrumentTags))
        {
            foreach (var (tagKey, keyValuePair) in perInstrumentTags)
            {
                // already resolved above, since instrument-specific tags take priority over general ones
                if (!tagKeys.Contains(tagKey))
                {
                    tagList.Add(keyValuePair);
                }
            }
        }
    }

    bool TryGetTag(string tagKey, string? instrumentName, out KeyValuePair<string, object?> keyValuePair)
    {
        if (instrumentName != null &&
            instrumentTags.TryGetValue(instrumentName, out var perInstrumentTags) &&
            perInstrumentTags.TryGetValue(tagKey, out keyValuePair))
        {
            return true;
        }

        return tags.TryGetValue(tagKey, out keyValuePair);
    }
}
