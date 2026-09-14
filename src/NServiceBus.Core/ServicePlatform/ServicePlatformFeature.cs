#nullable enable

namespace NServiceBus;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;
using NServiceBus.Transport;

class ServicePlatformFeature : Feature
{
    public ServicePlatformFeature()
        => Defaults(ServicePlatform.Defaults);

    protected override void Setup(FeatureConfigurationContext context)
        => context.Services.AddSingleton(
                serviceProvider => new ServicePlatform(
                    ServicePlatform.GetConfiguration(context.Settings),
                    serviceProvider.GetRequiredService<IMessageDispatcher>(),
                    serviceProvider.GetService<ReceiveAddresses>()
                )
            );
}
