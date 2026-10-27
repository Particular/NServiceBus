namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains information about queues this endpoint is using.
/// </summary>
public partial class QueueBindings
{
    /// <summary>
    /// Returns the transport addresses registered with <see cref="BindSending"/>, such as the error and audit queues.
    /// </summary>
    /// <remarks>
    /// The collection does not contain every destination the endpoint sends to. Routed destinations, such as the input queues of other endpoints, are not included.
    /// </remarks>
    public IReadOnlyCollection<string> SendingAddresses => sendingAddresses;

    /// <summary>
    /// Declares that this endpoint will be using queue with address <paramref name="transportAddress" /> for sending.
    /// </summary>
    /// <param name="transportAddress">The address of the queue.</param>
    public void BindSending(string transportAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportAddress);
        sendingAddresses.Add(transportAddress);
    }

    readonly List<string> sendingAddresses = [];
}