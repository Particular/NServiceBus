namespace NServiceBus.Testing;

using System;
using Pipeline;
using Transport;

/// <summary>
/// A testable implementation for <see cref="ITransportReceiveContext" />.
/// </summary>
public partial class TestableTransportReceiveContext : TestableBehaviorContext, ITransportReceiveContext
{
    /// <summary>
    /// The physical message being processed.
    /// </summary>
    public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>());
}