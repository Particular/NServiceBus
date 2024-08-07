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
    Dictionary<string, KeyValuePair<string, object?>>? tags;

    /// <summary>
    /// Adds the specified tag and value to the collection if not already present.
    /// </summary>
    /// <param name="tagKey">The tag to add.</param>
    /// <param name="value">The value assigned to the tag.</param>
    public void Add(string tagKey, object value)
    {
        tags ??= [];
        // We are using tryAdd to mitigate multiple logical messages transmitted in a single physical message
        tags.TryAdd(tagKey, new(tagKey, value));
    }

    /// <summary>
    /// Applies the specified tag to the <paramref name="tagList"/>.
    /// </summary>
    /// <param name="tagList">The tagList to apply the specified tag to.</param>
    /// <param name="tagKey">The tag to add to the <paramref name="tagList"/>.</param>
    public void ApplyTag(ref TagList tagList, string tagKey)
    {
        if (tags == null)
        {
            return;
        }

        if (tags.TryGetValue(tagKey, out var keyValuePair))
        {
            tagList.Add(keyValuePair);
        }
    }

    /// <summary>
    /// Applies the specified tags to the <paramref name="tagList"/>.
    /// </summary>
    /// <param name="tagList">The tagList to add the tags to.</param>
    /// <param name="tagKeys">The collection of tag keys to apply to the <paramref name="tagList"/>.</param>
    public void ApplyTags(ref TagList tagList, ReadOnlySpan<string> tagKeys)
    {
        if (tags == null || tagKeys.IsEmpty)
        {
            return;
        }

        foreach (var tagKey in tagKeys)
        {
            if (tags.TryGetValue(tagKey, out var keyValuePair))
            {
                tagList.Add(keyValuePair);
            }
        }
    }
}