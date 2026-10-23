#nullable enable

namespace NServiceBus.Transport;

using System;
using Settings;

/// <summary>
/// Provides access to header pooling related settings.
/// </summary>
public static class HeaderPoolingSettingsExtensions
{
    /// <summary>
    /// Returns the header dictionary pool configured for the endpoint. Transports can use it to
    /// participate in pooling only when the hosting endpoint has pooling enabled; when pooling is
    /// disabled this is an always-allocating pool whose return is a no-op.
    /// </summary>
    public static HeaderPool GetHeaderPool(this IReadOnlySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Get<HeaderPool>();
    }
}