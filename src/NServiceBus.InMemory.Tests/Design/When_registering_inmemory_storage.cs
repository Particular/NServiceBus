namespace NServiceBus.Persistence.InMemory;

using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class When_registering_inmemory_storage
{
    [Test]
    public void Should_use_dependency_injection_then_explicit_configuration_then_shared_default()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProviderStorage = new InMemoryStorage();
        serviceCollection.AddSingleton(serviceProviderStorage);

        InMemoryStorageRuntime.Configure(serviceCollection, configuredStorage: new InMemoryStorage());

        using var provider = serviceCollection.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<InMemoryStorage>(), Is.SameAs(serviceProviderStorage));

        var configuredServices = new ServiceCollection();
        var configuredStorage = new InMemoryStorage();

        InMemoryStorageRuntime.Configure(configuredServices, configuredStorage);

        using var configuredProvider = configuredServices.BuildServiceProvider();
        Assert.That(configuredProvider.GetRequiredService<InMemoryStorage>(), Is.SameAs(configuredStorage));

        var defaultServices = new ServiceCollection();

        InMemoryStorageRuntime.Configure(defaultServices, configuredStorage: null);

        using var defaultProvider = defaultServices.BuildServiceProvider();
        Assert.That(defaultProvider.GetRequiredService<InMemoryStorage>(), Is.SameAs(InMemoryStorageRuntime.SharedStorage));
    }
}