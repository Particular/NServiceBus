namespace NServiceBus.ObjectBuilder
{
    using Microsoft.Extensions.DependencyInjection;
    using System;

    static class ServiceCollectionExtensions
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
