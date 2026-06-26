#nullable enable

namespace NServiceBus;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class KeyedServiceProviderAdapter : IKeyedServiceProvider, ISupportRequiredService, IServiceProviderIsKeyedService, IDisposable, IAsyncDisposable
{
    public KeyedServiceProviderAdapter(IServiceProvider serviceProvider, object serviceKey, KeyedServiceCollectionAdapter serviceCollection, bool ownsProvider = false)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        this.serviceProvider = serviceProvider;
        serviceKeyedServiceKey = new KeyedServiceKey(serviceKey);
        anyKey = KeyedServiceKey.AnyKey(serviceKey);
        this.serviceCollection = serviceCollection;
        this.ownsProvider = ownsProvider;

        keyedScopeFactory = new KeyedServiceScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>(), serviceKeyedServiceKey, serviceCollection);
    }

    public bool IsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (IsServiceProvider(serviceType) || IsScopeFactory(serviceType) || IsServicesRequest(serviceType))
        {
            return true;
        }

        return ContainsLocalEndpointService(serviceType, serviceKeyedServiceKey) || ContainsRootEndpointKeyedService(serviceType) || ContainsRootService(serviceType);
    }

    public bool IsKeyedService(Type serviceType, object? serviceKey)
    {
        var computedKey = GetOrCreateComputedKey(serviceKey);
        return ContainsLocalEndpointService(serviceType, computedKey) || ContainsRootKeyedService(serviceType, GetBaseKeyOrServiceKey(serviceKey));
    }

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(serviceProvider);

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
            if (ContainsLocalEndpointService(serviceType, serviceKeyedServiceKey))
            {
                return serviceProvider.GetKeyedService(serviceType, GetLocalEndpointServiceKey(serviceKeyedServiceKey));
            }

            return ContainsRootEndpointKeyedService(serviceType) ? serviceProvider.GetKeyedService(serviceType, serviceKeyedServiceKey.BaseKey) : serviceProvider.GetService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (ContainsLocalEndpointService(itemType, serviceKeyedServiceKey))
        {
            return serviceProvider.GetKeyedServices(itemType, GetLocalEndpointServiceKey(serviceKeyedServiceKey));
        }

        return ContainsRootEndpointKeyedService(itemType) ? serviceProvider.GetKeyedServices(itemType, serviceKeyedServiceKey.BaseKey) : serviceProvider.GetServices(itemType);
    }

    public object GetRequiredService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(serviceProvider);

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
            if (ContainsLocalEndpointService(serviceType, serviceKeyedServiceKey))
            {
                return serviceProvider.GetRequiredKeyedService(serviceType, GetLocalEndpointServiceKey(serviceKeyedServiceKey));
            }

            return ContainsRootEndpointKeyedService(serviceType) ? serviceProvider.GetRequiredKeyedService(serviceType, serviceKeyedServiceKey.BaseKey) : serviceProvider.GetRequiredService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (ContainsLocalEndpointService(itemType, serviceKeyedServiceKey))
        {
            return serviceProvider.GetKeyedServices(itemType, GetLocalEndpointServiceKey(serviceKeyedServiceKey));
        }

        return ContainsRootEndpointKeyedService(itemType) ? serviceProvider.GetKeyedServices(itemType, serviceKeyedServiceKey.BaseKey) : serviceProvider.GetServices(itemType);
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (IsScopeFactory(serviceType))
        {
            return keyedScopeFactory;
        }

        var computedKey = GetOrCreateComputedKey(serviceKey);
        if (!IsServicesRequest(serviceType))
        {
            return ContainsLocalEndpointService(serviceType, computedKey)
                ? serviceProvider.GetKeyedService(serviceType, GetLocalEndpointServiceKey(computedKey))
                : serviceProvider.GetKeyedService(serviceType, GetBaseKeyOrServiceKey(serviceKey));
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (!Equals(computedKey, anyKey))
        {
            return ContainsLocalEndpointService(itemType, computedKey)
                ? serviceProvider.GetKeyedServices(itemType, GetLocalEndpointServiceKey(computedKey))
                : serviceProvider.GetKeyedServices(itemType, GetBaseKeyOrServiceKey(serviceKey));
        }

        return GetAllServices(serviceProvider, itemType);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (IsServiceProvider(serviceType))
        {
            return this;
        }

        if (IsScopeFactory(serviceType))
        {
            return keyedScopeFactory;
        }

        var computedKey = GetOrCreateComputedKey(serviceKey);
        if (!IsServicesRequest(serviceType))
        {
            return ContainsLocalEndpointService(serviceType, computedKey)
                ? serviceProvider.GetRequiredKeyedService(serviceType, GetLocalEndpointServiceKey(computedKey))
                : serviceProvider.GetRequiredKeyedService(serviceType, GetBaseKeyOrServiceKey(serviceKey));
        }

        var itemType = serviceType.GetGenericArguments()[0];
        if (!Equals(computedKey, anyKey))
        {
            return ContainsLocalEndpointService(itemType, computedKey)
                ? serviceProvider.GetKeyedServices(itemType, GetLocalEndpointServiceKey(computedKey))
                : serviceProvider.GetKeyedServices(itemType, GetBaseKeyOrServiceKey(serviceKey));
        }

        return GetAllServices(serviceProvider, itemType);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }

        if (!ownsProvider || serviceProvider is null)
        {
            return;
        }

        if (serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            serviceProvider = null;
        }
    }


    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }

        if (!ownsProvider)
        {
            return;
        }

        (serviceProvider as IDisposable)?.Dispose();
        serviceProvider = null;
    }

    // This is a convenience helper that in case someone throws in the KeyedServiceKey, it returns the base key, otherwise the original key
    // this allows resolving services by either the KeyedServiceKey or the base key and makes the experience consistent
    static object? GetBaseKeyOrServiceKey(object? serviceKey) => serviceKey is KeyedServiceKey key ? key.BaseKey : serviceKey;

    bool ContainsLocalEndpointService(Type serviceType, KeyedServiceKey key) => Equals(serviceKeyedServiceKey.BaseKey, key.BaseKey) && serviceCollection.ContainsLocalService(serviceType, key.ServiceKey);

    KeyedServiceKey GetLocalEndpointServiceKey(KeyedServiceKey key) => serviceCollection.GetLocalServiceKey(key.ServiceKey);

    bool ContainsRootEndpointKeyedService(Type serviceType) => ContainsRootKeyedService(serviceType, serviceKeyedServiceKey.BaseKey);

    bool ContainsRootService(Type serviceType)
    {
        var rootServiceProbe = serviceProvider?.GetService<IServiceProviderIsService>();
        if (rootServiceProbe?.IsService(serviceType) == true)
        {
            return true;
        }

        foreach (var descriptor in serviceCollection.Inner)
        {
            if (!descriptor.IsKeyedService && ServiceTypeMatches(descriptor.ServiceType, serviceType))
            {
                return true;
            }
        }

        return false;
    }

    bool ContainsRootKeyedService(Type serviceType, object? serviceKey)
    {
        var rootKeyedServiceProbe = serviceProvider?.GetService<IServiceProviderIsKeyedService>();
        if (rootKeyedServiceProbe?.IsKeyedService(serviceType, serviceKey) == true)
        {
            return true;
        }

        foreach (var descriptor in serviceCollection.Inner)
        {
            if (descriptor.IsKeyedService && ServiceTypeMatches(descriptor.ServiceType, serviceType) && Equals(serviceKey, descriptor.ServiceKey))
            {
                return true;
            }
        }

        return false;
    }

    KeyedServiceKey GetOrCreateComputedKey(object? serviceKey)
    {
        if (serviceKey is KeyedServiceKey key && Equals(serviceKeyedServiceKey.BaseKey, key.BaseKey))
        {
            return key;
        }

        return new KeyedServiceKey(serviceKeyedServiceKey, serviceKey);
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

    static bool IsServicesRequest(Type serviceType) => serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    static bool ServiceTypeMatches(Type registeredServiceType, Type requestedServiceType) => registeredServiceType == requestedServiceType || (requestedServiceType.IsGenericType && registeredServiceType == requestedServiceType.GetGenericTypeDefinition());
    static bool IsServiceProvider(Type serviceType) => serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService) || serviceType == typeof(IServiceProviderIsKeyedService) || serviceType == typeof(IServiceProviderIsService);
    static bool IsScopeFactory(Type serviceType) => serviceType == typeof(IServiceScopeFactory);

    int disposeSignaled;
    IServiceProvider? serviceProvider;
    readonly KeyedServiceKey serviceKeyedServiceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
    readonly bool ownsProvider;
    readonly KeyedServiceKey anyKey;
    readonly KeyedServiceScopeFactory keyedScopeFactory;
}