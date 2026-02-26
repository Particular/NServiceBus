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

    public IServiceCollection Inner
    {
        get;
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

        lock (Inner)
        {
            var keyedDescriptor = EnsureKeyedDescriptor(item);
            descriptors.Add(keyedDescriptor);
            Inner.Add(keyedDescriptor);
        }
    }

    public void Clear()
    {
        lock (Inner)
        {
            foreach (var descriptor in descriptors)
            {
                _ = Inner.Remove(descriptor);
            }

            descriptors.Clear();
            serviceTypes.Clear();
        }
    }

    public bool Contains(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return descriptors.Contains(item);
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => descriptors.CopyTo(array, arrayIndex);

    public IEnumerator<ServiceDescriptor> GetEnumerator() => descriptors.GetEnumerator();

    public int IndexOf(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return descriptors.IndexOf(item);
    }

    public void Insert(int index, ServiceDescriptor item) => throw new NotSupportedException("Inserting service descriptors at specific positions is not supported for multi endpoint services.");

    public bool Remove(ServiceDescriptor item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (Inner)
        {
            if (!descriptors.Remove(item))
            {
                return false;
            }

            _ = Inner.Remove(item);
            _ = serviceTypes.Remove(item.ServiceType);
        }
        return true;
    }

    public void RemoveAt(int index)
    {
        lock (Inner)
        {
            var descriptor = descriptors[index];
            descriptors.RemoveAt(index);
            _ = Inner.Remove(descriptor);
            _ = serviceTypes.Remove(descriptor.ServiceType);
        }
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

        serviceTypes.Add(keyedDescriptor.ServiceType);
        return keyedDescriptor;
    }

    static class UnsafeAccessor
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_implementationType")]
        public static extern ref Type GetImplementationType(ServiceDescriptor descriptor);
    }

    readonly List<ServiceDescriptor> descriptors = [];
    readonly HashSet<Type> serviceTypes = [];
    readonly ConcurrentDictionary<Type, ObjectFactory> factories = new();
}