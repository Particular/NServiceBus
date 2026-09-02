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
    internal const string TrimmingMessage = "AddMessageType<TMessage> relies on an NServiceBus source-generated interceptor to register the statically known message hierarchy without reflection when trimming is enabled. If this warning is reported, the interceptor was not used for this call; enable or restore the NServiceBus analyzer/source-generator tooling and use a statically known message type.";

    /// <summary>
    /// Registers the message type including its hierarchy of base types and implemented interfaces.
    /// </summary>
    /// <remarks>
    /// The type is checked against the endpoint's message conventions when the message metadata registry is initialized.
    /// This method registers metadata for a type that the conventions already identify as a message; it does not itself
    /// classify an arbitrary type as a message. Types that do not implement <see cref="IMessage"/>, <see cref="IEvent"/>,
    /// or <see cref="ICommand"/> (unobtrusive mode) need matching conventions defined with
    /// <see cref="EndpointConfiguration.Conventions()"/>.
    /// The hierarchy is inferred at runtime using reflection. Under trimming or NativeAOT the call is replaced by a
    /// source-generated, reflection-free registration that registers the statically known hierarchy instead.
    /// </remarks>
    [RequiresUnreferencedCode(TrimmingMessage)]
    public static void AddMessageType<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] TMessage>(this EndpointConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var messageMetadataRegistry = config.Settings.GetOrCreate<MessageMetadataRegistry>();
        messageMetadataRegistry.RegisterMessageTypes([typeof(TMessage)]);
    }
}