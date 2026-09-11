#nullable enable

namespace NServiceBus;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;

class ServicePlatformFeature : Feature
{
    public ServicePlatformFeature()
        => Defaults(ServicePlatform.Defaults);

    protected override void Setup(FeatureConfigurationContext context)
        => context.Services.AddSingleton(
                serviceProvider => ActivatorUtilities.CreateInstance<ServicePlatform>(
                    serviceProvider,
                    ServicePlatform.GetConfiguration(context.Settings)
                )
            );
}
