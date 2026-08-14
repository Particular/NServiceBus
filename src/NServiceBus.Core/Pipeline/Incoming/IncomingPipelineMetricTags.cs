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
        => tags.TryAdd(tagKey, new KeyValuePair<string, object?>(tagKey, value));

    /// <summary>
    /// Applies the specified tag to the <paramref name="tagList"/>, replacing any tag already in
    /// <paramref name="tagList"/> with the same key. When <paramref name="instrumentName"/> is provided and a tag
    /// was added for that instrument via <see cref="Add(string,object,string)"/>, that value is used instead of the
    /// general one added via <see cref="Add(string,object)"/>.
    /// </summary>
    /// <param name="tagList">The tagList to apply the specified tag to.</param>
    /// <param name="tagKey">The tag to add to the <paramref name="tagList"/>.</param>
    /// <param name="instrumentName">The instrument to prefer instrument-specific tags for, if any.</param>
    public void ApplyTag(ref TagList tagList, string tagKey, string? instrumentName = null)
        => ApplyTags(ref tagList, [tagKey], instrumentName: instrumentName);

    /// <summary>
    /// Applies the specified tags to the <paramref name="tagList"/>, replacing any tag already in
    /// <paramref name="tagList"/> with a matching key - so a caller can populate <paramref name="tagList"/> with its
    /// own computed defaults before calling this, and have any matching tag from this collection take precedence.
    /// General tags (from <see cref="Add(string,object)"/>) are only applied when their key is in
    /// <paramref name="tagKeys"/>. When <paramref name="instrumentName"/> is provided, every tag added for that
    /// instrument via <see cref="Add(string,object,string)"/> is applied unconditionally - regardless of whether its
    /// key is in <paramref name="tagKeys"/> - and takes precedence over a general tag with the same key: naming an
    /// instrument when adding a tag is already an explicit statement of intent for that one instrument, so callers
    /// don't also need to know about it to pull it in.
    /// </summary>
    /// <param name="tagList">The tagList to add the tags to.</param>
    /// <param name="tagKeys">The collection of tag keys to apply to the <paramref name="tagList"/>.</param>
    /// <param name="instrumentName">The instrument to apply instrument-specific tags for, if any.</param>
    public void ApplyTags(ref TagList tagList, ReadOnlySpan<string> tagKeys, string? instrumentName = null)
    {
        foreach (var tagKey in tagKeys)
        {
            if (tags.TryGetValue(tagKey, out var keyValuePair))
            {
                SetOrAdd(ref tagList, keyValuePair);
            }
        }

        if (instrumentName != null && instrumentTags.TryGetValue(instrumentName, out var perInstrumentTags))
        {
            foreach (var (_, keyValuePair) in perInstrumentTags)
            {
                SetOrAdd(ref tagList, keyValuePair);
            }
        }
    }

    // A caller may have already added a computed default for this key directly to tagList before calling
    // ApplyTag/ApplyTags. Replacing it in place - rather than appending a duplicate - is what lets a tag from this
    // collection act as an override regardless of how a consumer of the recorded measurement handles duplicate keys.
    static void SetOrAdd(ref TagList tagList, KeyValuePair<string, object?> tag)
    {
        for (var i = 0; i < tagList.Count; i++)
        {
            if (tagList[i].Key == tag.Key)
            {
                tagList[i] = tag;
                return;
            }
        }

        tagList.Add(tag);
    }
}
