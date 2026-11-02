#nullable enable

namespace NServiceBus.Features;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.ServicePlatform;
using NServiceBus.Transport;

/// <summary>
/// Used to configure the service platform feature.
/// </summary>
public sealed class ServicePlatform : Feature
{
    /// <summary>
    /// Create a new instance of the service platform feature.
    /// </summary>
    public ServicePlatform()
        => Defaults(MessagingBasedServicePlatformConnection.Defaults);

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
        => context.Services.AddSingleton(
                serviceProvider => (ServicePlatformConnection)new MessagingBasedServicePlatformConnection(
                    MessagingBasedServicePlatformConnection.GetConfiguration(context.Settings),
                    serviceProvider.GetRequiredService<IMessageDispatcher>(),
                    // HINT: ReceiveAddresses is only registered when the endpoint is configured to receive messages, so it is optional here.
                    serviceProvider.GetService<ReceiveAddresses>()
                )
            );
}
