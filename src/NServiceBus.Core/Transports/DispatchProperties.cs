#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;
using DelayedDelivery;
using Performance.TimeToBeReceived;

/// <summary>
/// Describes additional properties for an outgoing message.
/// </summary>
public class DispatchProperties : Dictionary<string, string>
{
    //These can't be changed to be backwards compatible with previous versions of the core
    static readonly string DoNotDeliverBeforeKeyName = "DeliverAt";
    static readonly string DelayDeliveryWithKeyName = "DelayDeliveryFor";
    static readonly string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

    Dictionary<string, object>? extensions;

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/>.
    /// </summary>
    public DispatchProperties()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/> an copies the values from the provided dictionary.
    /// </summary>
    public DispatchProperties(DispatchProperties properties) : base(properties)
    {
        if (properties.extensions is not null)
        {
            // TODO copy necessary?
            extensions = new Dictionary<string, object>(properties.extensions);
        }
    }

    /// <summary>
    /// Delay message delivery to a specific <see cref="DateTimeOffset"/>.
    /// </summary>
    public DoNotDeliverBefore? DoNotDeliverBefore
    {
        get => ContainsKey(DoNotDeliverBeforeKeyName)
            ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(this[DoNotDeliverBeforeKeyName]))
            : null;

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            this[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value.At);
        }
    }

    /// <summary>
    /// Delay message delivery by a certain <see cref="TimeSpan"/>.
    /// </summary>
    public DelayDeliveryWith? DelayDeliveryWith
    {
        get => ContainsKey(DelayDeliveryWithKeyName)
            ? new DelayDeliveryWith(TimeSpan.Parse(this[DelayDeliveryWithKeyName]))
            : null;

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            this[DelayDeliveryWithKeyName] = value.Delay.ToString();
        }
    }

    /// <summary>
    /// Discard the message after a certain period of time.
    /// </summary>
    public DiscardIfNotReceivedBefore? DiscardIfNotReceivedBefore
    {
        get => ContainsKey(DiscardIfNotReceivedBeforeKeyName)
            ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(this[DiscardIfNotReceivedBeforeKeyName]))
            : null;

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            this[DiscardIfNotReceivedBeforeKeyName] = value.MaxTime.ToString();
        }
    }

    /// <summary>
    /// Allows adding custom ephemeral data to the message that is not persisted.
    /// </summary>
    public Dictionary<string, object> Extensions => extensions ??= [];
}