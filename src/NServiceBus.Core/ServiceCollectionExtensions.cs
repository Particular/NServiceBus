namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    // Rename this class to ServiceCollectionExtensions when public version is removed from obsoletes.
    static class ServiceCollectionExtensionsInternal
    {
        public static void AddWithInterfaces(this IServiceCollection serviceCollection, Type serviceType, ServiceLifetime serviceLifetime)
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, serviceType, serviceLifetime));

            foreach (var interfaceType in serviceType.GetInterfaces())
            {
                serviceCollection.Add(new ServiceDescriptor(interfaceType, sp => sp.GetService(serviceType), serviceLifetime));
            }
        }
    }
}
