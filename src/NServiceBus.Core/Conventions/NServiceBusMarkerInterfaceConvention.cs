#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// A message convention that uses the built-in NServiceBus marker interfaces.
/// </summary>
public class NServiceBusMarkerInterfaceConvention : IMessageConvention
{
    /// <inheritdoc cref="IMessageConvention"/>
    public string Name => "NServiceBus Marker Interfaces";

    /// <inheritdoc cref="IMessageConvention"/>
    public bool IsCommandType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return typeof(ICommand).IsAssignableFrom(type) && typeof(ICommand) != type;
    }

    /// <inheritdoc cref="IMessageConvention"/>
    public bool IsEventType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return typeof(IEvent).IsAssignableFrom(type) && typeof(IEvent) != type;
    }

    /// <inheritdoc cref="IMessageConvention"/>
    public bool IsMessageType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return typeof(IMessage).IsAssignableFrom(type) &&
             typeof(IMessage) != type &&
             typeof(IEvent) != type &&
             typeof(ICommand) != type;
    }
}