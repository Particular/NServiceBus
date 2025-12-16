#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unicast;

/// <summary>
/// Provides extensions to manually register message handlers.
/// </summary>
public static class MessageHandlerRegistrationExtensions
{
    /// <summary>
    /// Registers a message handler.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "When trimming, this method will either be intercepted or throw an exception.")]
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "When trimming, this method will either be intercepted or throw an exception.")]
    public static void AddHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this EndpointConfiguration config) where THandler : IHandleMessages
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("This call requires a source generator. Add the [NServiceBusRegistrations] attribute to the calling method or class to enable the generator.");
        }

        var messageHandlerRegistry = config.Settings.GetOrCreate<MessageHandlerRegistry>();
        messageHandlerRegistry.AddHandler<THandler>();
    }
}