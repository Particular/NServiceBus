#nullable enable

namespace NServiceBus.Core.Tests.Host;

using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Settings;

[TestFixture]
public class KeyedServicesSettingsExtensionsTests
{
    [Test]
    public void Should_store_adapter_in_settings_and_return_it()
    {
        var settings = new SettingsHolder();
        var services = new ServiceCollection();

        var returned = settings.GetOrCreateKeyedServiceCollection(services, "endpoint-key");

        Assert.That(returned, Is.TypeOf<KeyedServiceCollectionAdapter>());

        var adapter = settings.GetOrDefault<KeyedServiceCollectionAdapter>();
        Assert.That(adapter, Is.SameAs(returned));
        Assert.That(adapter!.Inner, Is.SameAs(services));
        Assert.That(adapter.ServiceKey.BaseKey, Is.EqualTo("endpoint-key"));
    }

    [Test]
    public void Should_return_existing_adapter_on_repeat_call()
    {
        var settings = new SettingsHolder();
        var services = new ServiceCollection();

        var first = settings.GetOrCreateKeyedServiceCollection(services, "endpoint-key");
        var second = settings.GetOrCreateKeyedServiceCollection(services, "endpoint-key");

        Assert.That(second, Is.SameAs(first));
    }
}