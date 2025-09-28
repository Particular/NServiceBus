namespace NServiceBus.Core.Tests.Hosting;

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Transport.Learning;
using NUnit.Framework;

[TestFixture]
public class MultiEndpointTests
{
    [Test]
    public void Create_requires_at_least_one_endpoint()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => MultiEndpoint.Create(services, _ => { }));

        Assert.That(exception!.Message, Does.Contain("At least one endpoint must be configured."));
    }

    [Test]
    public void Create_registers_keyed_endpoint_services()
    {
        var services = new ServiceCollection();

        MultiEndpoint.Create(services, configuration =>
        {
            var sales = configuration.AddEndpoint("Sales");
            sales.UseTransport(new LearningTransport());
            sales.SendOnly();

            var shipping = configuration.AddEndpoint("Shipping");
            shipping.UseTransport(new LearningTransport());
            shipping.SendOnly();
        });

        var messageSessions = services.Where(descriptor => descriptor.ServiceType == typeof(IMessageSession)).ToList();
        Assert.That(messageSessions, Has.Count.EqualTo(2));
        Assert.That(messageSessions.Select(d => d.ServiceKey), Is.SupersetOf(new object[] { "Sales", "Shipping" }));

        var endpointInstances = services.Where(descriptor => descriptor.ServiceType == typeof(IEndpointInstance)).ToList();
        Assert.That(endpointInstances, Has.Count.EqualTo(2));
        Assert.That(endpointInstances.Select(d => d.ServiceKey), Is.SupersetOf(new object[] { "Sales", "Shipping" }));

        var lazySessions = services.Where(descriptor => descriptor.ServiceType == typeof(Lazy<IMessageSession>)).ToList();
        Assert.That(lazySessions, Has.Count.EqualTo(2));
        Assert.That(lazySessions.Select(d => d.ServiceKey), Is.SupersetOf(new object[] { "Sales", "Shipping" }));
    }

    [Test]
    public void Configuration_throws_for_duplicate_endpoint_names()
    {
        var configuration = new MultiEndpointConfiguration();

        configuration.AddEndpoint("Sales");

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.AddEndpoint("Sales"));

        Assert.That(exception!.Message, Does.Contain("already been added"));
    }

    [Test]
    public void Configuration_throws_for_duplicate_service_keys()
    {
        var configuration = new MultiEndpointConfiguration();

        configuration.AddEndpoint("custom", "Sales");

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.AddEndpoint("custom", "Shipping"));

        Assert.That(exception!.Message, Does.Contain("unique service key"));
    }

    [Test]
    public void Keyed_provider_accesses_endpoint_and_shared_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton("shared");

        var adapter = new KeyedServiceCollectionAdapter(services, "key");
        adapter.AddSingleton(new EndpointInstanceAccessor());

        var provider = services.BuildServiceProvider();
        var keyedProvider = new KeyedServiceProviderAdapter(provider, "key", adapter);

        Assert.That(keyedProvider.GetService(typeof(EndpointInstanceAccessor)), Is.Not.Null);
        Assert.That(keyedProvider.GetService(typeof(string)), Is.EqualTo("shared"));
    }
}
