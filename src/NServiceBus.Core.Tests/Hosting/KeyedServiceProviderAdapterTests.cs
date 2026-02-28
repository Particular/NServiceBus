#nullable enable

namespace NServiceBus.Core.Tests.Host;

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
}