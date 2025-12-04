namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

class KeyedServiceCollectionAdapter : IServiceCollection
{
    public KeyedServiceCollectionAdapter(IServiceCollection inner, object serviceKey)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(serviceKey);

        this.inner = inner;
        this.serviceKey = new KeyedServiceKey(serviceKey);
    }

    public ServiceDescriptor this[int index]
    {
        // we assume no more modifications can occur at this point and therefore read without a lock
        get => descriptors[index];
        set => throw new NotSupportedException("Replacing service descriptors is not supported for multi endpoint services.");
    }

    public int Count => descriptors.Count;

    public bool IsReadOnly => false;

    public void Add(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (inner)
        {
            var keyedDescriptor = EnsureKeyedDescriptor(item);
            descriptors.Add(keyedDescriptor);
            inner.Add(keyedDescriptor);
        }
    }

    public void Clear()
    {
        lock (inner)
        {
            foreach (var descriptor in descriptors)
            {
                _ = inner.Remove(descriptor);
            }

            descriptors.Clear();
            serviceTypes.Clear();
        }
    }

    public bool Contains(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // we assume no more modifications can occur at this point and therefore read without a lock
        return descriptors.Contains(item);
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => descriptors.CopyTo(array, arrayIndex);

    public IEnumerator<ServiceDescriptor> GetEnumerator() => descriptors.GetEnumerator(); // we assume no more modifications can occur at this point and therefore read without a lock

    public int IndexOf(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // we assume no more modifications can occur at this point and therefore read without a lock
        return descriptors.IndexOf(item);
    }

    public void Insert(int index, ServiceDescriptor item) => throw new NotSupportedException("Inserting service descriptors at specific positions is not supported for multi endpoint services.");

    public bool Remove(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (inner)
        {
            if (!descriptors.Remove(item))
            {
                return false;
            }

            _ = inner.Remove(item);
            _ = serviceTypes.Remove(item.ServiceType);
        }
        return true;
    }

    public void RemoveAt(int index)
    {
        lock (inner)
        {
            var descriptor = descriptors[index];
            descriptors.RemoveAt(index);
            _ = inner.Remove(descriptor);
            _ = serviceTypes.Remove(descriptor.ServiceType);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        // we assume no more modifications can occur at this point and therefore read without a lock
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
        ServiceDescriptor keyedDescriptor;
        if (descriptor.IsKeyedService)
        {
            if (descriptor.KeyedImplementationInstance is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, new KeyedServiceKey(serviceKey, descriptor.ServiceKey), descriptor.KeyedImplementationInstance);
            }
            else if (descriptor.KeyedImplementationFactory is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, new KeyedServiceKey(serviceKey, descriptor.ServiceKey), (serviceProvider, key) =>
                {
                    var resultingKey = key is null ? serviceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                    var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                    return descriptor.KeyedImplementationFactory!(keyedProvider, key);
                }, descriptor.Lifetime);
            }
            else if (descriptor.KeyedImplementationType is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, new KeyedServiceKey(serviceKey, descriptor.ServiceKey),
                    (serviceProvider, key) =>
                    {
                        var resultingKey = key is null ? serviceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                        var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                        return descriptor.Lifetime == ServiceLifetime.Singleton ? ActivatorUtilities.CreateInstance(keyedProvider, descriptor.KeyedImplementationType) :
                            factories.GetOrAdd(descriptor.KeyedImplementationType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes))(keyedProvider, []);
                    }, descriptor.Lifetime);
                // Crazy hack to work around generic constraint checks
                UnsafeAccessor.GetImplementationType(keyedDescriptor) = descriptor.KeyedImplementationType;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported keyed service descriptor configuration for service type '{descriptor.ServiceType}'.");
            }
        }
        else
        {
            if (descriptor.ImplementationInstance is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, serviceKey, descriptor.ImplementationInstance);
            }
            else if (descriptor.ImplementationFactory is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, serviceKey, (serviceProvider, key) =>
                {
                    var resultingKey = key is null ? serviceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                    var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                    return descriptor.ImplementationFactory!(keyedProvider);
                }, descriptor.Lifetime);
            }
            else if (descriptor.ImplementationType is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, serviceKey,
                    (serviceProvider, key) =>
                    {
                        var resultingKey = key is null ? serviceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                        var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                        return descriptor.Lifetime == ServiceLifetime.Singleton ? ActivatorUtilities.CreateInstance(keyedProvider, descriptor.ImplementationType) :
                            factories.GetOrAdd(descriptor.ImplementationType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes))(keyedProvider, []);
                    }, descriptor.Lifetime);
                // Crazy hack to work around generic constraint checks
                UnsafeAccessor.GetImplementationType(keyedDescriptor) = descriptor.ImplementationType;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported service descriptor configuration for service type '{descriptor.ServiceType}'.");
            }
        }

        serviceTypes.Add(keyedDescriptor.ServiceType);
        return keyedDescriptor;
    }

    static class UnsafeAccessor
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_implementationType")]
        public static extern ref Type GetImplementationType(ServiceDescriptor descriptor);
    }

    readonly IServiceCollection inner;
    readonly KeyedServiceKey serviceKey;
    readonly List<ServiceDescriptor> descriptors = [];
    readonly HashSet<Type> serviceTypes = [];
    readonly ConcurrentDictionary<Type, ObjectFactory> factories = new();
}