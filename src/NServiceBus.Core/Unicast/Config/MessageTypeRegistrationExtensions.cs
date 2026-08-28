#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Unicast.Messages;

/// <summary>
/// Provides extensions to manually register message types.
/// </summary>
public static class MessageTypeRegistrationExtensions
{
    /// <summary>
    /// Registers the message type including its hierarchy.
    /// </summary>
    /// <remarks>
    /// The hierarchy is inferred at runtime using reflection. Under trimming or NativeAOT the call is replaced by a
    /// source-generated, reflection-free registration that registers the statically known hierarchy instead.
    /// </remarks>
    [RequiresUnreferencedCode(MessageMetadataRegistry.TrimmingMessage)]
    public static void AddMessageType<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] TMessage>(this EndpointConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var messageMetadataRegistry = config.Settings.GetOrCreate<MessageMetadataRegistry>();
        messageMetadataRegistry.RegisterMessageTypes([typeof(TMessage)]);
    }
}
