#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class KeyedServiceProviderAdapterTests
{
    [Test]
    public void Disposing_adapter_should_not_dispose_underlying_service_provider()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        adapter.Dispose();

        var scopeFactory = adapter.GetRequiredService<IServiceScopeFactory>();
        Assert.DoesNotThrow(() => scopeFactory.CreateScope().Dispose());
    }

    [Test]
    public async Task Async_dispose_should_not_dispose_underlying_service_provider()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        await using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        await adapter.DisposeAsync();

        var scopeFactory = adapter.GetRequiredService<IServiceScopeFactory>();
        Assert.DoesNotThrow(() => scopeFactory.CreateScope().Dispose());
    }

    [Test]
    public void GetRequiredService_should_resolve_root_service_registered_with_endpoint_key()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootEndpointComponent>("endpoint");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var component = adapter.GetRequiredService(typeof(IComponent));

        Assert.That(component, Is.InstanceOf<RootEndpointComponent>());
    }

    [Test]
    public void GetService_should_resolve_root_service_registered_with_endpoint_key()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootEndpointComponent>("endpoint");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var component = adapter.GetService(typeof(IComponent));

        Assert.That(component, Is.InstanceOf<RootEndpointComponent>());
    }

    [Test]
    public void IsKeyedService_should_return_true_for_local_keyed_service()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsKeyedService(typeof(IComponent), "component");

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsKeyedService_should_return_true_for_root_keyed_service()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootKeyedComponent>("component");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsKeyedService(typeof(IComponent), "component");

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsKeyedService_should_use_root_keyed_service_probe()
    {
        var providerServices = new ServiceCollection();
        providerServices.AddKeyedSingleton<IComponent, RootKeyedComponent>("component");
        var adapterServices = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(adapterServices, "endpoint");
        using var rootProvider = providerServices.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsKeyedService(typeof(IComponent), "component");

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsKeyedService_should_return_false_when_service_is_not_available()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsKeyedService(typeof(IComponent), "component");

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsKeyedService_should_return_false_for_local_service_with_different_endpoint_key()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsKeyedService(typeof(IComponent), new KeyedServiceKey("other-endpoint", "component"));

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsService_should_return_true_for_local_endpoint_service()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddSingleton<IComponent, LocalComponent>();
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsService(typeof(IComponent));

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsService_should_return_true_for_root_service_registered_with_endpoint_key()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootEndpointComponent>("endpoint");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsService(typeof(IComponent));

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsService_should_return_true_for_root_unkeyed_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IComponent, RootComponent>();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsService(typeof(IComponent));

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsService_should_use_root_service_probe()
    {
        var providerServices = new ServiceCollection();
        providerServices.AddSingleton<IComponent, RootComponent>();
        var adapterServices = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(adapterServices, "endpoint");
        using var rootProvider = providerServices.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsService(typeof(IComponent));

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsService_should_return_false_when_service_is_not_available()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var result = adapter.IsService(typeof(IComponent));

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsService_should_return_true_for_adapter_services()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter.IsService(typeof(IServiceProvider)), Is.True);
            Assert.That(adapter.IsService(typeof(ISupportRequiredService)), Is.True);
            Assert.That(adapter.IsService(typeof(IServiceProviderIsService)), Is.True);
            Assert.That(adapter.IsService(typeof(IServiceProviderIsKeyedService)), Is.True);
            Assert.That(adapter.IsService(typeof(IServiceScopeFactory)), Is.True);
            Assert.That(adapter.IsService(typeof(IEnumerable<IComponent>)), Is.True);
        }
    }

    [Test]
    public void GetService_and_GetRequiredService_should_return_adapter_services()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter.GetService(typeof(IServiceProvider)), Is.SameAs(adapter));
            Assert.That(adapter.GetRequiredService(typeof(ISupportRequiredService)), Is.SameAs(adapter));
            Assert.That(adapter.GetService(typeof(IServiceProviderIsService)), Is.SameAs(adapter));
            Assert.That(adapter.GetRequiredService(typeof(IServiceProviderIsKeyedService)), Is.SameAs(adapter));
            Assert.That(adapter.GetService(typeof(IServiceScopeFactory)), Is.Not.Null);
            Assert.That(adapter.GetRequiredService(typeof(IServiceScopeFactory)), Is.Not.Null);
        }
    }

    [Test]
    public void GetRequiredKeyedService_should_prefer_local_keyed_service_for_matching_key()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootKeyedComponent>("component");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var component = adapter.GetRequiredKeyedService(typeof(IComponent), "component");

        Assert.That(component, Is.InstanceOf<LocalKeyedComponent>());
    }

    [Test]
    public void GetKeyedService_should_prefer_local_keyed_service_for_matching_key()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootKeyedComponent>("component");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var component = adapter.GetKeyedService(typeof(IComponent), "component");

        Assert.That(component, Is.InstanceOf<LocalKeyedComponent>());
    }

    [Test]
    public void GetRequiredKeyedService_should_fallback_to_root_keyed_service_when_local_key_does_not_match()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootKeyedComponent>("root-component");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("local-component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var component = adapter.GetRequiredKeyedService(typeof(IComponent), "root-component");

        Assert.That(component, Is.InstanceOf<RootKeyedComponent>());
    }

    [Test]
    public void GetKeyedService_should_return_null_when_keyed_service_is_not_available()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var component = adapter.GetKeyedService(typeof(IComponent), "component");

        Assert.That(component, Is.Null);
    }

    [Test]
    public void GetService_and_GetRequiredService_should_resolve_local_endpoint_enumerables()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddSingleton<IComponent, LocalComponent>();
        keyedServices.AddSingleton<IComponent, LocalKeyedComponent>();
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var components = ((IEnumerable<IComponent>)adapter.GetService(typeof(IEnumerable<IComponent>))!).ToList();
        var requiredComponents = ((IEnumerable<IComponent>)adapter.GetRequiredService(typeof(IEnumerable<IComponent>))).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(components, Has.Count.EqualTo(2));
            Assert.That(requiredComponents, Has.Count.EqualTo(2));
            Assert.That(requiredComponents, Is.EquivalentTo(components));
        }
    }

    [Test]
    public void GetService_and_GetRequiredService_should_resolve_root_endpoint_keyed_enumerables()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IComponent, RootEndpointComponent>("endpoint");
        services.AddKeyedSingleton<IComponent, SecondRootEndpointComponent>("endpoint");
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var components = ((IEnumerable<IComponent>)adapter.GetService(typeof(IEnumerable<IComponent>))!).ToList();
        var requiredComponents = ((IEnumerable<IComponent>)adapter.GetRequiredService(typeof(IEnumerable<IComponent>))).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(components, Has.Count.EqualTo(2));
            Assert.That(requiredComponents, Has.Count.EqualTo(2));
            Assert.That(requiredComponents, Is.EquivalentTo(components));
        }
    }

    [Test]
    public void GetKeyedService_and_GetRequiredKeyedService_should_resolve_local_keyed_enumerables()
    {
        var services = new ServiceCollection();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("component");
        keyedServices.AddKeyedSingleton<IComponent, SecondLocalKeyedComponent>("component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var components = ((IEnumerable<IComponent>)adapter.GetKeyedService(typeof(IEnumerable<IComponent>), "component")!).ToList();
        var requiredComponents = ((IEnumerable<IComponent>)adapter.GetRequiredKeyedService(typeof(IEnumerable<IComponent>), "component")).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(components, Has.Count.EqualTo(2));
            Assert.That(requiredComponents, Has.Count.EqualTo(2));
            Assert.That(requiredComponents, Is.EquivalentTo(components));
        }
    }

    [Test]
    public void GetKeyedService_and_GetRequiredKeyedService_should_resolve_all_services_for_any_key()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IComponent, RootComponent>();
        var keyedServices = new KeyedServiceCollectionAdapter(services, "endpoint");
        keyedServices.AddSingleton<IComponent, LocalComponent>();
        keyedServices.AddKeyedSingleton<IComponent, LocalKeyedComponent>("component");
        using var rootProvider = services.BuildServiceProvider();
        var adapter = new KeyedServiceProviderAdapter(rootProvider, "endpoint", keyedServices);

        var components = ((IEnumerable<IComponent>)adapter.GetKeyedService(typeof(IEnumerable<IComponent>), KeyedServiceKey.Any)!).ToList();
        var requiredComponents = ((IEnumerable<IComponent>)adapter.GetRequiredKeyedService(typeof(IEnumerable<IComponent>), KeyedServiceKey.Any)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(components, Has.Count.EqualTo(3));
            Assert.That(requiredComponents, Has.Count.EqualTo(3));
            Assert.That(requiredComponents, Is.EquivalentTo(components));
        }
    }

    interface IComponent;

    class LocalComponent : IComponent;

    class LocalKeyedComponent : IComponent;

    class SecondLocalKeyedComponent : IComponent;

    class RootEndpointComponent : IComponent;

    class SecondRootEndpointComponent : IComponent;

    class RootComponent : IComponent;

    class RootKeyedComponent : IComponent;
}