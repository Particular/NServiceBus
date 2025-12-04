namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class KeyedServiceProviderAdapter : IKeyedServiceProvider, ISupportRequiredService, IServiceProviderIsKeyedService, IDisposable, IAsyncDisposable
{
    public KeyedServiceProviderAdapter(IServiceProvider serviceProvider, object serviceKey, KeyedServiceCollectionAdapter serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        this.serviceProvider = serviceProvider;
        serviceKeyedServiceKey = new KeyedServiceKey(serviceKey);
        anyKey = KeyedServiceKey.AnyKey(serviceKey);
        this.serviceCollection = serviceCollection;

        keyedScopeFactory = new KeyedServiceScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>(), serviceKeyedServiceKey, serviceCollection);
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

        if (IsScopeFactory(serviceType))
        {
            return keyedScopeFactory;
        }

        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, serviceKeyedServiceKey)
                ? serviceProvider.GetKeyedService(serviceType, serviceKeyedServiceKey)
                : serviceProvider.GetService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, serviceKeyedServiceKey) ? serviceProvider.GetKeyedServices(itemType, serviceKeyedServiceKey) : serviceProvider.GetServices(itemType);
    }

    static bool IsServicesRequest(Type serviceType) => serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    public object GetRequiredService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (IsScopeFactory(serviceType))
        {
            return keyedScopeFactory;
        }

        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, serviceKeyedServiceKey)
                ? serviceProvider.GetRequiredKeyedService(serviceType, serviceKeyedServiceKey)
                : serviceProvider.GetRequiredService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(itemType, serviceKeyedServiceKey) ? serviceProvider.GetKeyedServices(itemType, serviceKeyedServiceKey) : serviceProvider.GetServices(itemType);
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (IsScopeFactory(serviceType))
        {
            return keyedScopeFactory;
        }

        var computedServiceKey = new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);
        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, computedServiceKey)
                ? serviceProvider.GetKeyedService(serviceType, computedServiceKey)
                : serviceProvider.GetKeyedService(serviceType, serviceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (!Equals(computedServiceKey, anyKey))
        {
            return IsKeyedService(itemType, computedServiceKey)
                ? serviceProvider.GetKeyedServices(itemType, computedServiceKey)
                : serviceProvider.GetKeyedServices(itemType, serviceKey);
        }

        return GetAllServices(serviceProvider, itemType);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (IsScopeFactory(serviceType))
        {
            return keyedScopeFactory;
        }

        var computedServiceKey = new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);
        if (!IsServicesRequest(serviceType))
        {
            return IsKeyedService(serviceType, computedServiceKey)
                ? serviceProvider.GetRequiredKeyedService(serviceType, computedServiceKey)
                : serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (!Equals(computedServiceKey, anyKey))
        {
            return IsKeyedService(itemType, computedServiceKey)
                ? serviceProvider.GetKeyedServices(itemType, computedServiceKey)
                : serviceProvider.GetKeyedServices(itemType, serviceKey);
        }

        return GetAllServices(serviceProvider, itemType);
    }

    public void Dispose() => (serviceProvider as IDisposable)?.Dispose();

    public ValueTask DisposeAsync()
    {
        if (serviceProvider is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        Dispose();
        return ValueTask.CompletedTask;
    }

    static object GetAllServices(IServiceProvider serviceProvider, Type itemType)
    {
        Type genericEnumerable = typeof(List<>).MakeGenericType(itemType);
        var services = (IList)Activator.CreateInstance(genericEnumerable)!;
        foreach (var service in serviceProvider.GetServices(itemType).Concat(serviceProvider.GetKeyedServices(itemType, KeyedService.AnyKey)))
        {
            _ = services.Add(service);
        }
        return services;
    }

    static bool IsServiceProvider(Type serviceType) => serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService) || serviceType == typeof(IServiceProviderIsKeyedService) || serviceType == typeof(IServiceProviderIsService);
    static bool IsScopeFactory(Type serviceType) => serviceType == typeof(IServiceScopeFactory);

    readonly IServiceProvider serviceProvider;
    readonly KeyedServiceKey serviceKeyedServiceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
    readonly KeyedServiceKey anyKey;
    readonly KeyedServiceScopeFactory keyedScopeFactory;
}