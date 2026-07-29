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
        return typeof(ICommand).IsAssignableFrom(type) && !IsMarkerType(type);
    }

    /// <inheritdoc cref="IMessageConvention"/>
    public bool IsEventType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return typeof(IEvent).IsAssignableFrom(type) && !IsMarkerType(type);
    }

    /// <inheritdoc cref="IMessageConvention"/>
    public bool IsMessageType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return typeof(IMessage).IsAssignableFrom(type) && !IsMarkerType(type);
    }

    internal static bool IsMarkerType(Type type) => type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);

    internal static bool IsMarkerType(string typeName) => typeName == typeof(IMessage).FullName || typeName == typeof(IEvent).FullName || typeName == typeof(ICommand).FullName;
}