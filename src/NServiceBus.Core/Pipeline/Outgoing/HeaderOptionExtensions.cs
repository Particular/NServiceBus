﻿namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Extensibility;

/// <summary>
/// Extensions to the options to provide ways to set message headers.
/// </summary>
public static class HeaderOptionExtensions
{
    /// <summary>
    /// Allows headers to be set for the outgoing message.
    /// </summary>
    /// <param name="options">The options to extend.</param>
    /// <param name="key">The header key.</param>
    /// <param name="value">The header value.</param>
    public static void SetHeader(this ExtendableOptions options, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        options.OutgoingHeaders[key] = value;
    }

    /// <summary>
    /// Returns all headers set by <see cref="SetHeader" /> on the outgoing message.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetHeaders(this ExtendableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ReadOnlyDictionary<string, string>(options.OutgoingHeaders);
    }
}