using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace NServiceBus.AcceptanceTests.Diagnostics;

public static class ActivityTestingExtensions
{

    public static void VerifyTag(this ImmutableDictionary<string, string> tags, string tagName, string expectedValue)
    {
        Assert.IsTrue(tags.TryGetValue(tagName, out var tagValue), $"Tags should contain key '{tagName}'");
        Assert.AreEqual(expectedValue, tagValue, $"Tag value with key '{tags}' is incorrect");
    }

    /// <summary>
    /// Checks tags for duplicate tag keys.
    /// </summary>
    public static void VerifyUniqueTags(this Activity activity)
    {
        ImmutableList<KeyValuePair<string, string>> tagsList = activity.Tags.ToImmutableList();

        if (tagsList.Count < 2)
        {
            return;
        }

        var sortedTags = tagsList.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Key, b.Key));

        for (int i = 0; i < sortedTags.Count - 1; i++)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Equals(sortedTags[i].Key, sortedTags[i+1].Key))
            {
                Assert.Fail($"duplicate tag found: {sortedTags[i].Key}. Tags should be unique.");
            }
        }
    }
}