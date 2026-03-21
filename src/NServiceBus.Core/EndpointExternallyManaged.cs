namespace NServiceBus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

static class EndpointExternallyManaged
{
    internal static ExternallyManagedContainerHost Create(EndpointConfiguration configuration,
        IServiceCollection serviceCollection)
    {
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        serviceCollection.TryAddSingleton<IMessageSession>(endpointCreator.MessageSession);

        return new ExternallyManagedContainerHost(endpointCreator);
    }
}