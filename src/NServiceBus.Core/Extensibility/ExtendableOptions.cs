namespace NServiceBus.Extensibility;

using System.Collections.Generic;
using Transport;

/// <summary>
/// Allows the users to control how message session operations are performed.
/// </summary>
/// <remarks>
/// The behavior of this class is exposed via extension methods.
/// </remarks>
public abstract class ExtendableOptions
{
    internal const string OperationPropertiesKey = "NServiceBus.OperationProperties";

    /// <summary>
    /// Creates an instance of an extendable option.
    /// </summary>
    protected ExtendableOptions()
    {
        Context = new ContextBag();
        OutgoingHeaders = [];
    }

    internal DispatchProperties DispatchProperties { get; } = [];

    internal ContextBag Context { get; }

    internal string UserDefinedMessageId { get; set; }

    internal Dictionary<string, string> OutgoingHeaders { get; }
}