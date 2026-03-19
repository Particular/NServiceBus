namespace NServiceBus.Transport;

using System.Collections.Generic;

/// <summary>
/// Properties received from the transport that can be propagated to outgoing dispatch operations.
/// Transports populate this with native message metadata that should survive audit and error operations.
/// </summary>
public class ReceiveProperties : Dictionary<string, string>
{
    /// <summary>
    /// Creates an empty instance of <see cref="ReceiveProperties"/>.
    /// </summary>
    public ReceiveProperties() { }

    /// <summary>
    /// Creates a copy of the provided dictionary.
    /// </summary>
    public ReceiveProperties(IDictionary<string, string> dictionary) : base(dictionary) { }
}