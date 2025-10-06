namespace NServiceBus.Core.Tests.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
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

            sales.TypesToScanInternal([]);
            sales.UseSerialization<SystemJsonSerializer>();
            sales.UseTransport(new LearningTransport());
            sales.SendOnly();

            var shipping = configuration.AddEndpoint("Shipping", c => c.TypesToScanInternal([]));
            shipping.TypesToScanInternal([]);
            shipping.UseSerialization<SystemJsonSerializer>();
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
    public async Task DemoTest()
    {
        var services = new ServiceCollection();

        var startable = MultiEndpoint.Create(services, configuration =>
        {
            var sales = configuration.AddEndpoint("Sales");

            sales.TypesToScanInternal([typeof(MySalesCommandHandler)]);
            sales.UseSerialization<SystemJsonSerializer>();
            sales.UseTransport(new LearningTransport());

            var shipping = configuration.AddEndpoint("Shipping", c => c.TypesToScanInternal([]));
            shipping.TypesToScanInternal([typeof(MySalesEventHandler)]);
            shipping.UseSerialization<SystemJsonSerializer>();
            shipping.UseTransport(new LearningTransport());
        });

        var serviceProvider = services.BuildServiceProvider();
        await startable.Start(serviceProvider);

        var salesSession = serviceProvider.GetKeyedService<IMessageSession>("Sales");
        var shippingSession = serviceProvider.GetKeyedService<IMessageSession>("Shipping");

        await salesSession.SendLocal(new MySalesCommand { Message = "Hello from sales" });
        await shippingSession.SendLocal(new MySalesCommand { Message = "Should result in no handlers found" });
        await shippingSession.Send("Sales", new MySalesCommand { Message = "Hello from shipping" });

        await salesSession.Publish(new MySalesEvent());


        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    public class MySalesCommand : ICommand
    {
        public string Message { get; set; }
    }

    public class MySalesEvent : IEvent
    {
    }

    class MySalesCommandHandler : IHandleMessages<MySalesCommand>
    {
        public Task Handle(MySalesCommand message, IMessageHandlerContext context)
        {
            Console.WriteLine(message.Message);
            return Task.CompletedTask;
        }
    }

    class MySalesEventHandler : IHandleMessages<MySalesEvent>
    {
        public Task Handle(MySalesEvent message, IMessageHandlerContext context)
        {
            Console.WriteLine("Got the my sales event");
            return Task.CompletedTask;
        }
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

    [Test]
    public void Implementation_factory_gets_keyed_provider()
    {
        var services = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(services, "endpoint");

        IServiceProvider? capturedProvider = null;

        adapter.AddSingleton(sp =>
        {
            capturedProvider = sp;
            return new object();
        });

        var provider = services.BuildServiceProvider();
        var keyedProvider = new KeyedServiceProviderAdapter(provider, "endpoint", adapter);

        Assert.That(keyedProvider.GetService(typeof(object)), Is.Not.Null);
        Assert.That(capturedProvider, Is.TypeOf<KeyedServiceProviderAdapter>());
    }

    [Test]
    public async Task Implementation_factory_with_scope_gets_keyed_provider()
    {
        var services = new ServiceCollection();

        var salesAdapter = new KeyedServiceCollectionAdapter(services, "sales");
        int salesScopeCount = 0;
        salesAdapter.AddSingleton("Hello from sales");
        salesAdapter.AddScoped(sp => new ScopeDependency(sp.GetRequiredService<string>() + salesScopeCount++));

        var billingAdapter = new KeyedServiceCollectionAdapter(services, "billing");
        int billingScopeCount = 0;
        billingAdapter.AddSingleton("Hello from billing");
        billingAdapter.AddScoped(sp => new ScopeDependency(sp.GetRequiredService<string>() + billingScopeCount++));

        var provider = services.BuildServiceProvider();

        ScopeDependency salesScope1Dependency;
        ScopeDependency salesScope2Dependency;
        ScopeDependency billingScope1Dependency;
        ScopeDependency billingScope2Dependency;
        await using (var scope1 = provider.CreateAsyncScope())
        await using (var scope2 = provider.CreateAsyncScope())
        {
            salesScope1Dependency = scope1.ServiceProvider.GetRequiredKeyedService<ScopeDependency>("sales");
            var salesScope1DependencySecondResolve = scope1.ServiceProvider.GetRequiredKeyedService<ScopeDependency>("sales");
            salesScope2Dependency = scope2.ServiceProvider.GetRequiredKeyedService<ScopeDependency>("sales");
            billingScope1Dependency = scope1.ServiceProvider.GetRequiredKeyedService<ScopeDependency>("billing");
            var billingScope1DependencySecondResolve = scope1.ServiceProvider.GetRequiredKeyedService<ScopeDependency>("billing");
            billingScope2Dependency = scope2.ServiceProvider.GetRequiredKeyedService<ScopeDependency>("billing");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(salesScope1Dependency.Disposed, Is.Zero);
                Assert.That(salesScope1Dependency.Value, Is.EqualTo("Hello from sales0"));
                Assert.That(salesScope1Dependency, Is.EqualTo(salesScope1DependencySecondResolve));
                Assert.That(salesScope1Dependency, Is.Not.EqualTo(salesScope2Dependency));
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(billingScope1Dependency.Disposed, Is.Zero);
                Assert.That(billingScope1Dependency.Value, Is.EqualTo("Hello from billing0"));
                Assert.That(billingScope1Dependency, Is.EqualTo(billingScope1DependencySecondResolve));
                Assert.That(billingScope1Dependency, Is.Not.EqualTo(billingScope2Dependency));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(salesScope2Dependency.Disposed, Is.Zero);
                Assert.That(salesScope2Dependency.Value, Is.EqualTo("Hello from sales1"));
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(billingScope2Dependency.Disposed, Is.Zero);
                Assert.That(billingScope2Dependency.Value, Is.EqualTo("Hello from billing1"));
            }
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(salesScope1Dependency.Disposed, Is.EqualTo(1));
            Assert.That(salesScope2Dependency.Disposed, Is.EqualTo(1));
            Assert.That(billingScope1Dependency.Disposed, Is.EqualTo(1));
            Assert.That(billingScope2Dependency.Disposed, Is.EqualTo(1));
        }
    }

    sealed class ScopeDependency(string value) : IDisposable
    {
        public string Value { get; } = value;

        public int Disposed { get; private set; }

        public void Dispose() => Disposed++;
    }
}