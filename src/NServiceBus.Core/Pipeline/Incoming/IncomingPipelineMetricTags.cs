#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;

sealed class IncomingPipelineMetricTags
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

    // This can be made a readonly span with CSharp 13
    public void ApplyTags(ref TagList tagList, params string[] tagKeys)
    {
        if (tags == null)
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