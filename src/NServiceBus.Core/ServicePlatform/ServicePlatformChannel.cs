#nullable enable

namespace NServiceBus.ServicePlatform;

using System.Text.Json.Serialization.Metadata;

/// <summary>
/// A channel for sending information to the Particular Service Platform.
/// </summary>
public abstract class ServicePlatformChannel
{
    /// <summary>
    /// Creates an object for sending messages to the Particular Service Platform.
    /// </summary>
    public abstract ServicePlatformSender<TMessage> CreateSender<TMessage>(JsonTypeInfo<TMessage> jsonTypeInfo);
}
