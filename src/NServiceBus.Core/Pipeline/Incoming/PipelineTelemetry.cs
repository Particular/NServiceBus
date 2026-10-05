#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// The tracing and metrics state of the message currently being processed: the incoming pipeline activity and the
/// metric tags that can be applied to a metric throughout the incoming processing pipeline.
/// </summary>
/// <remarks>
/// One instance lives on the root <see cref="Extensibility.ContextBag"/> of a message and is reached through
/// <see cref="Extensibility.ContextBag.Telemetry"/> as a typed field, so no key lookup or parent walk is involved.
/// It is created for every incoming message, whether or not any listener or instrument is enabled. The two tags the
/// pipeline itself always adds get dedicated fields so that the common path allocates nothing beyond this object.
/// Everything else goes into dictionaries that are only allocated on first use.
/// </remarks>
sealed class PipelineTelemetry : IMetricsTags
{
    /// <summary>
    /// The activity of the incoming pipeline, when one was started. Per message, so it lives here; the outgoing
    /// pipeline activity is per operation and stays on the outgoing context, see <see cref="ActivityExtensions"/>.
    /// </summary>
    public Activity? IncomingActivity { get; set; }

    string? messageType;
    string? messageHandlerTypes;
    Dictionary<string, KeyValuePair<string, object?>>? tags;
    Dictionary<string, Dictionary<string, KeyValuePair<string, object?>>>? instrumentTags;

    /// <inheritdoc />
    /// <remarks>
    /// Unlike <see cref="Add(string,object)"/>, this overwrites rather than keeping the first value, and the tag
    /// isn't visible to other instruments applying tags from this collection.
    /// </remarks>
    public void AddOrOverride(string tagKey, object value, string instrumentName)
    {
        instrumentTags ??= [];

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
        switch (tagKey)
        {
            case MeterTags.MessageType when value is string stringValue:
                messageType ??= stringValue;
                return;
            case MeterTags.MessageHandlerTypes when value is string stringValue:
                messageHandlerTypes ??= stringValue;
                return;
            default:
                break;
        }

        tags ??= [];
        tags.TryAdd(tagKey, new KeyValuePair<string, object?>(tagKey, value));
    }

    /// <summary>
    /// Applies the specified tags to the <paramref name="tagList"/>, replacing any tag already in
    /// <paramref name="tagList"/> with a matching key - so a caller can populate <paramref name="tagList"/> with its
    /// own computed defaults before calling this, and have any matching tag from this collection take precedence.
    /// General tags (from <see cref="Add(string,object)"/>) are only applied when their key is in
    /// <paramref name="tagKeys"/>. When <paramref name="instrumentName"/> is provided, every tag added for that
    /// instrument via <see cref="AddOrOverride"/> is applied unconditionally - regardless of whether its
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
            switch (tagKey)
            {
                case MeterTags.MessageType when messageType is not null:
                    SetOrAdd(ref tagList, new KeyValuePair<string, object?>(tagKey, messageType));
                    break;
                case MeterTags.MessageHandlerTypes when messageHandlerTypes is not null:
                    SetOrAdd(ref tagList, new KeyValuePair<string, object?>(tagKey, messageHandlerTypes));
                    break;
                default:
                    if (tags is not null && tags.TryGetValue(tagKey, out var keyValuePair))
                    {
                        SetOrAdd(ref tagList, keyValuePair);
                    }

                    break;
            }
        }

        if (instrumentName != null && instrumentTags is not null && instrumentTags.TryGetValue(instrumentName, out var perInstrumentTags))
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
