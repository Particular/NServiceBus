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
            return IsKeyedService(serviceType, serviceKey)
                ? inner.GetKeyedService(serviceType, serviceKey)
                : inner.GetService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(serviceType, serviceKey) ? inner.GetKeyedServices(itemType, serviceKey) : inner.GetServices(serviceType);
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
            return IsKeyedService(serviceType, serviceKey)
                ? inner.GetRequiredKeyedService(serviceType, serviceKey)
                : inner.GetRequiredService(serviceType);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(serviceType, serviceKey) ? inner.GetKeyedServices(itemType, serviceKey) : inner.GetRequiredService(serviceType);
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(ISupportRequiredService))
        {
            return this;
        }

        var computedServiceKey = $"{this.serviceKey}{serviceKey}";

        if (serviceType == typeof(IServiceScopeFactory))
        {
            var scopeFactory = inner.GetRequiredService<IServiceScopeFactory>();
            return new KeyedServiceScopeFactory(scopeFactory, this.serviceKey, serviceCollection);
        }

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.IEnumerable<>))
        {
            return inner.GetRequiredKeyedService(serviceType, computedServiceKey);
        }

        var itemType = serviceType.GetGenericArguments()[0];
        return IsKeyedService(serviceType, computedServiceKey) ? inner.GetKeyedServices(itemType, computedServiceKey) : inner.GetRequiredService(serviceType);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) => throw new NotImplementedException();

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
    readonly object serviceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
}