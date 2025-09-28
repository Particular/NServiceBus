#nullable enable

namespace NServiceBus;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

class KeyedServiceCollectionAdapter : IServiceCollection
{
    public KeyedServiceCollectionAdapter(IServiceCollection inner, object serviceKey)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(serviceKey);

        this.inner = inner;
        this.serviceKey = serviceKey;
    }

    public ServiceDescriptor this[int index]
    {
        get => descriptors[index];
        set => throw new NotSupportedException("Replacing service descriptors is not supported for multi endpoint services.");
    }

    public int Count => descriptors.Count;

    public bool IsReadOnly => false;

    public void Add(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var keyedDescriptor = EnsureKeyedDescriptor(item);
        descriptors.Add(keyedDescriptor);
        inner.Add(keyedDescriptor);
    }

    public void Clear()
    {
        foreach (var descriptor in descriptors)
        {
            _ = inner.Remove(descriptor);
        }

        descriptors.Clear();
        serviceTypes.Clear();
    }

    public bool Contains(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return descriptors.Contains(item);
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        descriptors.CopyTo(array, arrayIndex);
    }

    public IEnumerator<ServiceDescriptor> GetEnumerator() => descriptors.GetEnumerator();

    public int IndexOf(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return descriptors.IndexOf(item);
    }

    public void Insert(int index, ServiceDescriptor item)
    {
        throw new NotSupportedException("Inserting service descriptors at specific positions is not supported for multi endpoint services.");
    }

    public bool Remove(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!descriptors.Remove(item))
        {
            return false;
        }

        _ = inner.Remove(item);
        _ = serviceTypes.Remove(item.ServiceType);
        return true;
    }

    public void RemoveAt(int index)
    {
        var descriptor = descriptors[index];
        descriptors.RemoveAt(index);
        _ = inner.Remove(descriptor);
        _ = serviceTypes.Remove(descriptor.ServiceType);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceTypes.Contains(serviceType))
        {
            return true;
        }

        if (serviceType.IsGenericType)
        {
            var definition = serviceType.GetGenericTypeDefinition();
            return serviceTypes.Contains(definition);
        }

        return false;
    }

    ServiceDescriptor EnsureKeyedDescriptor(ServiceDescriptor descriptor)
    {
        if (descriptor.IsKeyedService)
        {
            if (!Equals(descriptor.ServiceKey, serviceKey))
            {
                throw new InvalidOperationException("Endpoint scoped registrations must use the endpoint service key.");
            }

            serviceTypes.Add(descriptor.ServiceType);
            return descriptor;
        }

        ServiceDescriptor keyedDescriptor;

        if (descriptor.ImplementationInstance is not null)
        {
            keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationInstance);
        }
        else if (descriptor.ImplementationFactory is not null)
        {
            keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, serviceKey, (serviceProvider, key) =>
            {
                var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, key ?? serviceKey, this);
                return descriptor.ImplementationFactory!(keyedProvider);
            }, descriptor.Lifetime);
        }
        else if (descriptor.ImplementationType is not null)
        {
            keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationType, descriptor.Lifetime);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported service descriptor configuration for service type '{descriptor.ServiceType}'.");
        }

        serviceTypes.Add(keyedDescriptor.ServiceType);
        return keyedDescriptor;
    }

    readonly IServiceCollection inner;
    readonly object serviceKey;
    readonly List<ServiceDescriptor> descriptors = [];
    readonly HashSet<Type> serviceTypes = [];
}

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

        var keyed = ServiceProviderKeyedServiceExtensions.GetKeyedService(inner, serviceType, serviceKey);
        if (keyed != null)
        {
            return keyed;
        }

        if (serviceCollection.ContainsService(serviceType))
        {
            return keyed;
        }

        return inner.GetService(serviceType);
    }

    readonly IServiceProvider inner;
    readonly object serviceKey;
    readonly KeyedServiceCollectionAdapter serviceCollection;
}

class KeyedServiceScopeFactory(IServiceScopeFactory innerFactory, object serviceKey, KeyedServiceCollectionAdapter serviceCollection) : IServiceScopeFactory
{
    public IServiceScope CreateScope()
    {
        var innerScope = innerFactory.CreateScope();
        return new KeyedServiceScope(innerScope, serviceKey, serviceCollection);
    }

    class KeyedServiceScope : IServiceScope, IAsyncDisposable
    {
        public KeyedServiceScope(IServiceScope innerScope, object serviceKey, KeyedServiceCollectionAdapter serviceCollection)
        {
            ArgumentNullException.ThrowIfNull(innerScope);

            this.innerScope = innerScope;
            ServiceProvider = new KeyedServiceProviderAdapter(innerScope.ServiceProvider, serviceKey, serviceCollection);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose() => innerScope.Dispose();

        public ValueTask DisposeAsync()
        {
            if (innerScope is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            innerScope.Dispose();
            return ValueTask.CompletedTask;
        }

        readonly IServiceScope innerScope;
    }
}
