#nullable enable

namespace NServiceBus.ServicePlatform;
/// <summary>
/// Provides communication channels between the endpoint and the Particular Service Platform.
/// </summary>
public abstract class ServicePlatformConnection
{
    /// <summary>
    /// A communication channel between the endpoint and the Primary ServiceControl instance.
    /// </summary>
    public abstract ServicePlatformChannel PrimaryInstance { get; }
}
