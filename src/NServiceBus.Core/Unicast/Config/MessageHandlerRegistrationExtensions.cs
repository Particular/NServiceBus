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

        var messageHandlerRegistry = config.Settings.GetOrCreate<MessageHandlerRegistry>();
        messageHandlerRegistry.AddHandler<THandler>();
    }

    /// <summary>
    /// Registers all message handlers from an assembly, defined by a literal string. Requires source generator support.
    /// </summary>
    public static void AddHandlersFromAssembly(this EndpointConfiguration config, string assemblyName) =>
        throw new NotImplementedException("Requires source generator support");

    /// <summary>
    /// Registers all message handlers from an assembly, defined by a literal string. Requires source generator support.
    /// </summary>
    public static void AddHandlersFromNamespace(this EndpointConfiguration config, string namespaceName) =>
        throw new NotImplementedException("Requires source generator support");
}