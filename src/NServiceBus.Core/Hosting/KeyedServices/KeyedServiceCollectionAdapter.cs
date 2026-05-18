#nullable enable

namespace NServiceBus;

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

        Inner = inner;
        ServiceKey = new KeyedServiceKey(serviceKey);
    }

    public KeyedServiceKey ServiceKey { get; }

    public IServiceCollection Inner { get; }

    public ServiceDescriptor this[int index]
    {
        get => originalDescriptors[index];
        set => throw new NotSupportedException("Replacing service descriptors is not supported for multi endpoint services.");
    }

    public int Count => originalDescriptors.Count;

    public bool IsReadOnly => false;

    public void Add(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var keyedDescriptor = EnsureKeyedDescriptor(item);
        originalDescriptors.Add(item);
        keyedDescriptors.Add(keyedDescriptor);
        Inner.Add(keyedDescriptor);
    }

    public void Clear()
    {
        foreach (var descriptor in keyedDescriptors)
        {
            _ = Inner.Remove(descriptor);
        }

        keyedDescriptors.Clear();
        originalDescriptors.Clear();
        serviceTypeCounts.Clear();
    }

    public bool Contains(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return originalDescriptors.Contains(item) || keyedDescriptors.Contains(item);
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => originalDescriptors.CopyTo(array, arrayIndex);

    public IEnumerator<ServiceDescriptor> GetEnumerator() => originalDescriptors.GetEnumerator();

    public int IndexOf(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return originalDescriptors.IndexOf(item);
    }

    public void Insert(int index, ServiceDescriptor item) => throw new NotSupportedException("Inserting service descriptors at specific positions is not supported for multi endpoint services.");

    public bool Remove(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var index = originalDescriptors.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        var keyedDescriptor = keyedDescriptors[index];
        originalDescriptors.RemoveAt(index);
        keyedDescriptors.RemoveAt(index);
        _ = Inner.Remove(keyedDescriptor);
        DecrementServiceTypeCount(keyedDescriptor.ServiceType);
        return true;
    }

    public void RemoveAt(int index)
    {
        var keyedDescriptor = keyedDescriptors[index];
        keyedDescriptors.RemoveAt(index);
        originalDescriptors.RemoveAt(index);
        _ = Inner.Remove(keyedDescriptor);
        DecrementServiceTypeCount(keyedDescriptor.ServiceType);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceTypeCounts.ContainsKey(serviceType))
        {
            return true;
        }

        if (!serviceType.IsGenericType)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return serviceTypeCounts.ContainsKey(definition);
    }

    ServiceDescriptor EnsureKeyedDescriptor(ServiceDescriptor descriptor)
    {
        ServiceDescriptor keyedDescriptor;
        if (descriptor.IsKeyedService)
        {
            if (descriptor.KeyedImplementationInstance is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, new KeyedServiceKey(ServiceKey, descriptor.ServiceKey), descriptor.KeyedImplementationInstance);
            }
            else if (descriptor.KeyedImplementationFactory is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, new KeyedServiceKey(ServiceKey, descriptor.ServiceKey), (serviceProvider, key) =>
                {
                    var resultingKey = key is null ? ServiceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                    var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                    return descriptor.KeyedImplementationFactory!(keyedProvider, key);
                }, descriptor.Lifetime);
            }
            else if (descriptor.KeyedImplementationType is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, new KeyedServiceKey(ServiceKey, descriptor.ServiceKey),
                    (serviceProvider, key) =>
                    {
                        var resultingKey = key is null ? ServiceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                        var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                        return descriptor.Lifetime == ServiceLifetime.Singleton ? ActivatorUtilities.CreateInstance(keyedProvider, descriptor.KeyedImplementationType) :
                            factories.GetOrAdd(descriptor.KeyedImplementationType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes))(keyedProvider, []);
                    }, descriptor.Lifetime);
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
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, ServiceKey, descriptor.ImplementationInstance);
            }
            else if (descriptor.ImplementationFactory is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, ServiceKey, (serviceProvider, key) =>
                {
                    var resultingKey = key is null ? ServiceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                    var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                    return descriptor.ImplementationFactory!(keyedProvider);
                }, descriptor.Lifetime);
            }
            else if (descriptor.ImplementationType is not null)
            {
                keyedDescriptor = new ServiceDescriptor(descriptor.ServiceType, ServiceKey,
                    (serviceProvider, key) =>
                    {
                        var resultingKey = key is null ? ServiceKey : key as KeyedServiceKey ?? new KeyedServiceKey(key);
                        var keyedProvider = new KeyedServiceProviderAdapter(serviceProvider, resultingKey, this);
                        return descriptor.Lifetime == ServiceLifetime.Singleton ? ActivatorUtilities.CreateInstance(keyedProvider, descriptor.ImplementationType) :
                            factories.GetOrAdd(descriptor.ImplementationType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes))(keyedProvider, []);
                    }, descriptor.Lifetime);
                UnsafeAccessor.GetImplementationType(keyedDescriptor) = descriptor.ImplementationType;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported service descriptor configuration for service type '{descriptor.ServiceType}'.");
            }
        }

        if (!serviceTypeCounts.TryAdd(keyedDescriptor.ServiceType, 1))
        {
            serviceTypeCounts[keyedDescriptor.ServiceType]++;
        }

        return keyedDescriptor;
    }

    void DecrementServiceTypeCount(Type serviceType)
    {
        if (!serviceTypeCounts.TryGetValue(serviceType, out var count))
        {
            return;
        }

        if (count <= 1)
        {
            _ = serviceTypeCounts.Remove(serviceType);
            return;
        }

        serviceTypeCounts[serviceType] = count - 1;
    }

    static class UnsafeAccessor
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_implementationType")]
        public static extern ref Type GetImplementationType(ServiceDescriptor descriptor);
    }

    readonly List<ServiceDescriptor> originalDescriptors = [];
    readonly List<ServiceDescriptor> keyedDescriptors = [];
    readonly Dictionary<Type, int> serviceTypeCounts = [];
    readonly ConcurrentDictionary<Type, ObjectFactory> factories = new();
}