namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using NUnit.Framework;

public static class ActivityTestingExtensions
{
    public static void VerifyTag(this ImmutableDictionary<string, string> tags, string tagName, string expectedValue)
    {
        Assert.That(tags.TryGetValue(tagName, out var tagValue), Is.True, $"Tags should contain key '{tagName}'");
        Assert.That(tagValue, Is.EqualTo(expectedValue), $"Tag value with key '{tagName}' is incorrect");
    }

    /// <summary>
    /// Checks tags for duplicate tag keys.
    /// </summary>
    public static void VerifyUniqueTags(this Activity activity)
    {
        var tagsList = activity.Tags.ToImmutableList();

        if (tagsList.Count < 2)
        {
            return;
        }

        var sortedTags = tagsList.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Key, b.Key));

        for (int i = 0; i < sortedTags.Count - 1; i++)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Equals(sortedTags[i].Key, sortedTags[i + 1].Key))
            {
                Assert.Fail($"duplicate tag found: {sortedTags[i].Key}. Tags should be unique.");
            }
        }
    }
}