#nullable enable
namespace NServiceBus.AcceptanceTesting.Support;

using System;
using Microsoft.Extensions.DependencyInjection;

class KeyedServiceProviderAdapter : IServiceProvider, ISupportRequiredService, IServiceProviderIsKeyedService
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

    public bool IsService(Type serviceType) => serviceCollection.ContainsService(serviceType);
    public bool IsKeyedService(Type serviceType, object? serviceKey) => serviceCollection.ContainsService(serviceType) && Equals(this.serviceKey, serviceKey);

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService))
        {
            return this;
        }

        if (serviceType == typeof(IServiceScopeFactory))
        {
            var scopeFactory = inner.GetService<IServiceScopeFactory>();
            if (scopeFactory != null)
            {
                return new KeyedServiceScopeFactory(scopeFactory, serviceKey, serviceCollection);
            }
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return serviceCollection.ContainsService(serviceType)
                ? inner.GetKeyedService(serviceType, serviceKey)
                : inner.GetService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return serviceCollection.ContainsService(serviceType) ? inner.GetKeyedServices(itemType, serviceKey) : inner.GetServices(serviceType);

    }

    public object GetRequiredService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService))
        {
            return this;
        }

        if (serviceType == typeof(IServiceScopeFactory))
        {
            var scopeFactory = inner.GetRequiredService<IServiceScopeFactory>();
            return new KeyedServiceScopeFactory(scopeFactory, serviceKey, serviceCollection);
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return serviceCollection.ContainsService(serviceType)
                ? inner.GetRequiredKeyedService(serviceType, serviceKey)
                : inner.GetRequiredService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return serviceCollection.ContainsService(serviceType) ? inner.GetKeyedServices(itemType, serviceKey) : inner.GetRequiredService(serviceType);

    }

    readonly IServiceProvider inner;
    readonly object serviceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
}