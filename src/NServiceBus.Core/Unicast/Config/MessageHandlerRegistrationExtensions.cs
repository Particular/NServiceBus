#nullable enable
namespace NServiceBus;

using System;
using Unicast;

/// <summary>
/// Provides extensions to manually register message handlers.
/// </summary>
public static class MessageHandlerRegistrationExtensions
{
    /// <summary>
    /// Registers a message handler.
    /// </summary>
    public static void AddHandler<THandler>(this EndpointConfiguration config) where THandler : IHandleMessages
    {
        ArgumentNullException.ThrowIfNull(config);

        var registry = config.Settings.GetOrCreate<MessageHandlerRegistry>();
        registry.AddHandler<THandler>();
    }
}