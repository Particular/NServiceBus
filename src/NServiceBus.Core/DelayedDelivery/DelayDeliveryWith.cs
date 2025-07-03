﻿#nullable enable

namespace NServiceBus.DelayedDelivery;

using System;

/// <summary>
/// Represent a constraint that the message can't be delivered before the specified delay has elapsed.
/// </summary>
public class DelayDeliveryWith
{
    /// <summary>
    /// Initializes a new instance of <see cref="DelayDeliveryWith" />.
    /// </summary>
    /// <param name="delay">How long to delay the delivery of the message.</param>
    public DelayDeliveryWith(TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        Delay = delay;
    }

    /// <summary>
    /// The requested delay.
    /// </summary>
    public TimeSpan Delay { get; }
}