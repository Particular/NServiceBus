#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Unicast;

/// <summary>
/// Provides extensions to manually register message handlers.
/// </summary>
public static class MessageHandlerRegistrationExtensions
{
    /// <summary>
    /// Registers a message handler.
    /// </summary>
    [RequiresUnreferencedCode(MessageHandlerRegistry.TrimmingMessage)]
    public static void AddHandler<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Handler)] THandler>(this EndpointConfiguration config) where THandler : IHandleMessages
    {
        ArgumentNullException.ThrowIfNull(config);

        var messageHandlerRegistry = config.Settings.GetOrCreate<MessageHandlerRegistry>();
        messageHandlerRegistry.AddHandler<THandler>();
    }
}