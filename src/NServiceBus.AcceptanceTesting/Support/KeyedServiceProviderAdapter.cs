#nullable enable
namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

class KeyedServiceProviderAdapter : IServiceProvider
{
    public KeyedServiceProviderAdapter(IServiceProvider inner, object serviceKey, KeyedServiceCollectionAdapter serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        this.inner = inner;
        this.serviceKey = serviceKey;
        this.serviceCollection = serviceCollection;
    }

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceScopeFactory))
        {
            var scopeFactory = inner.GetService<IServiceScopeFactory>();
            if (scopeFactory != null)
            {
                return new KeyedServiceScopeFactory(scopeFactory, serviceKey, serviceCollection);
            }
        }

        if (serviceType.IsGenericType &&
            serviceType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
        {
            var itemType = serviceType.GetGenericArguments()[0];

            var keyedServices = inner.GetKeyedServices(itemType, serviceKey);
            if (keyedServices.Any() || serviceCollection.ContainsService(serviceType))
            {
                return keyedServices;
            }

            return inner.GetServices(serviceType);
        }

        var keyed = inner.GetKeyedService(serviceType, serviceKey);
        if (keyed != null || serviceCollection.ContainsService(serviceType))
        {
            return keyed;
        }

        return inner.GetService(serviceType);
    }

    readonly IServiceProvider inner;
    readonly object serviceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
}