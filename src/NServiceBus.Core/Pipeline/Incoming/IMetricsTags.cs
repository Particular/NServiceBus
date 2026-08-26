#nullable enable

namespace NServiceBus;

/// <summary>
/// The tags applied to the metrics reported for the message currently being processed.
/// </summary>
public interface IMetricsTags
{
    /// <summary>
    /// Adds the specified tag and value to <paramref name="instrumentName"/>, overwriting any value previously added
    /// for that instrument and tag key, and taking precedence over the value NServiceBus reports for that tag.
    /// </summary>
    /// <remarks>
    /// Tags are scoped to a single instrument because a tag value is frequently only valid for one measurement (for
    /// example, which handler just ran) rather than a fact that holds for every metric reported for the message. For
    /// the same reason a later call for the same instrument and tag key replaces an earlier one.
    /// </remarks>
    /// <param name="tagKey">The tag to add.</param>
    /// <param name="value">The value assigned to the tag.</param>
    /// <param name="instrumentName">The name of the instrument the tag applies to.</param>
    void AddOrOverride(string tagKey, object value, string instrumentName);
}
