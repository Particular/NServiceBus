#nullable enable

namespace NServiceBus;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;
using NServiceBus.ServicePlatform;
using NServiceBus.Transport;

class ServicePlatformFeature : Feature
{
    public ServicePlatformFeature()
        => Defaults(MessagingBasedServicePlatformConnection.Defaults);

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
