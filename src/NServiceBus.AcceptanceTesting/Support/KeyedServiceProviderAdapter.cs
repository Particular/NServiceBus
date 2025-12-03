#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class KeyedServiceProviderAdapter : IKeyedServiceProvider, ISupportRequiredService, IServiceProviderIsKeyedService, IDisposable, IAsyncDisposable
{
    public KeyedServiceProviderAdapter(IServiceProvider inner, object serviceKey, KeyedServiceCollectionAdapter serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        this.inner = inner;
        serviceKeyedServiceKey = new KeyedServiceKey(serviceKey);
        this.serviceCollection = serviceCollection;
    }

    public bool IsService(Type serviceType) => serviceCollection.ContainsService(serviceType);

    public bool IsKeyedService(Type serviceType, object? serviceKey)
    {
        if (!serviceCollection.ContainsService(serviceType))
        {
            return false;
        }

        if (serviceKey is KeyedServiceKey key)
        {
            return Equals(serviceKeyedServiceKey.BaseKey, key.BaseKey);
        }

        return false;
    }

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
                return new KeyedServiceScopeFactory(scopeFactory, serviceKeyedServiceKey, serviceCollection);
            }
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return IsKeyedService(serviceType, serviceKeyedServiceKey)
                ? inner.GetKeyedService(serviceType, serviceKeyedServiceKey)
                : inner.GetService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, serviceKeyedServiceKey) ? inner.GetKeyedServices(itemType, serviceKeyedServiceKey) : inner.GetServices(itemType);
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
            return new KeyedServiceScopeFactory(scopeFactory, serviceKeyedServiceKey, serviceCollection);
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return IsKeyedService(serviceType, serviceKeyedServiceKey)
                ? inner.GetRequiredKeyedService(serviceType, serviceKeyedServiceKey)
                : inner.GetRequiredService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, serviceKeyedServiceKey) ? inner.GetKeyedServices(itemType, serviceKeyedServiceKey) : inner.GetServices(itemType);
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService))
        {
            return this;
        }

        var computedServiceKey = new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);

        if (serviceType == typeof(IServiceScopeFactory))
        {
            var scopeFactory = inner.GetRequiredService<IServiceScopeFactory>();
            return new KeyedServiceScopeFactory(scopeFactory, serviceKeyedServiceKey, serviceCollection);
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return IsKeyedService(serviceType, computedServiceKey)
                ? inner.GetKeyedService(serviceType, computedServiceKey)
                : inner.GetKeyedService(serviceType, serviceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, computedServiceKey) ? inner.GetKeyedServices(itemType, computedServiceKey) : inner.GetKeyedServices(itemType, serviceKey);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService))
        {
            return this;
        }

        var computedServiceKey = new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);

        if (serviceType == typeof(IServiceScopeFactory))
        {
            var scopeFactory = inner.GetRequiredService<IServiceScopeFactory>();
            return new KeyedServiceScopeFactory(scopeFactory, serviceKeyedServiceKey, serviceCollection);
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return IsKeyedService(serviceType, computedServiceKey)
                ? inner.GetRequiredKeyedService(serviceType, computedServiceKey)
                : inner.GetRequiredKeyedService(serviceType, serviceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, computedServiceKey) ? inner.GetKeyedServices(itemType, computedServiceKey) : inner.GetKeyedServices(itemType, serviceKey);
    }

    public void Dispose() => (inner as IDisposable)?.Dispose();

    public ValueTask DisposeAsync()
    {
        if (inner is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        Dispose();
        return ValueTask.CompletedTask;
    }

    readonly IServiceProvider inner;
    readonly KeyedServiceKey serviceKeyedServiceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
}