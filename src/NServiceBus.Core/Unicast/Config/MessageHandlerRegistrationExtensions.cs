#nullable enable
namespace NServiceBus;

using System;
using Unicast;
using Unicast.Messages;

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

        var messageHandlerRegistry = config.Settings.GetOrCreate<MessageHandlerRegistry>();
        messageHandlerRegistry.AddHandler<THandler>();

        var messageMetadataRegistry = config.Settings.GetOrCreate<MessageMetadataRegistry>();
        messageMetadataRegistry.RegisterMessageType(typeof(THandler));
    }
}