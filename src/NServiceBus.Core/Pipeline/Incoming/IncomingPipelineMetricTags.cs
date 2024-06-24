#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;

public sealed class IncomingPipelineMetricTags
{
    Dictionary<string, KeyValuePair<string, object?>>? tags;
    public void Add(string tagKey, object value)
    {
        tags ??= [];
        tags.Add(tagKey, new(tagKey, value));
    }

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