namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        anyKey = KeyedServiceKey.AnyKey(serviceKey);
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

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (TryGetScopeFactory(serviceType, out object? scopeFactory))
        {
            return scopeFactory;
        }

        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, serviceKeyedServiceKey)
                ? inner.GetKeyedService(serviceType, serviceKeyedServiceKey)
                : inner.GetService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, serviceKeyedServiceKey) ? inner.GetKeyedServices(itemType, serviceKeyedServiceKey) : inner.GetServices(itemType);
    }

    static bool IsServicesRequest(Type serviceType) => serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    public object GetRequiredService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (TryGetScopeFactory(serviceType, out object? scopeFactory))
        {
            return scopeFactory;
        }

        if (!IsServicesRequest(serviceType))
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

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (TryGetScopeFactory(serviceType, out object? scopeFactory))
        {
            return scopeFactory;
        }

        var computedServiceKey = new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);
        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, computedServiceKey)
                ? inner.GetKeyedService(serviceType, computedServiceKey)
                : inner.GetKeyedService(serviceType, serviceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (!Equals(computedServiceKey, anyKey))
        {
            return IsKeyedService(itemType, computedServiceKey)
                ? inner.GetKeyedServices(itemType, computedServiceKey)
                : inner.GetKeyedServices(itemType, serviceKey);
        }

        Type genericEnumerable = typeof(List<>).MakeGenericType(itemType);
        var services = (IList)Activator.CreateInstance(genericEnumerable)!;
        foreach (var service in inner.GetServices(itemType).Concat(inner.GetKeyedServices(itemType, KeyedService.AnyKey)))
        {
            services.Add(service);
        }
        return services;
    }

    static bool IsServiceProvider(Type serviceType) => serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService) || serviceType == typeof(IServiceProviderIsKeyedService) || serviceType == typeof(IServiceProviderIsService);

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (TryGetScopeFactory(serviceType, out object? scopeFactory))
        {
            return scopeFactory;
        }

        var computedServiceKey = new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);
        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, computedServiceKey)
                ? inner.GetRequiredKeyedService(serviceType, computedServiceKey)
                : inner.GetRequiredKeyedService(serviceType, serviceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (!Equals(computedServiceKey, anyKey))
        {
            return IsKeyedService(itemType, computedServiceKey)
                ? inner.GetKeyedServices(itemType, computedServiceKey)
                : inner.GetKeyedServices(itemType, serviceKey);
        }

        Type genericEnumerable = typeof(List<>).MakeGenericType(itemType);
        var services = (IList)Activator.CreateInstance(genericEnumerable)!;
        foreach (var service in inner.GetServices(itemType).Concat(inner.GetKeyedServices(itemType, KeyedService.AnyKey)))
        {
            services.Add(service);
        }
        return services;
    }

    bool TryGetScopeFactory(Type serviceType, [NotNullWhen(true)] out object? scopeFactory)
    {
        if (serviceType != typeof(IServiceScopeFactory))
        {
            scopeFactory = null;
            return false;
        }

        scopeFactory = new KeyedServiceScopeFactory(inner.GetRequiredService<IServiceScopeFactory>(), serviceKeyedServiceKey, serviceCollection);
        return true;
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
    readonly KeyedServiceKey anyKey;
}