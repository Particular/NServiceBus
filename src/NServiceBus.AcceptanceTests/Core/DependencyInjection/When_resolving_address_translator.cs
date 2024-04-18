namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_resolving_address_translator : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_available_after_the_endpoint_is_started()
    {
        var serviceCollection = new ServiceCollection();
        string translatedAddress = null;

        await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            b.ToCreateInstance(
                config => EndpointWithExternallyManagedContainer.Create(config, serviceCollection),
                async (configured, ct) =>
                {
                    var serviceProvider = serviceCollection.BuildServiceProvider();

                    // HINT: Resolve before start
                    var transportAddressResolver = serviceProvider.GetRequiredService<ITransportAddressResolver>();

                    var endpoint = await configured.Start(serviceProvider, ct);

                    translatedAddress = transportAddressResolver.ToTransportAddress(new QueueAddress("SomeAddress"));

                    return endpoint;
                }))
            .Done(_ => !string.IsNullOrEmpty(translatedAddress))
            .Run();

        Assert.That(translatedAddress, Is.Not.Null);
    }

    [Test]
    public async Task Should_throw_meaningful_exception_when_resolved_before_endpoint_started()
    {
        var serviceCollection = new ServiceCollection();
        Exception thrownException = null;

        await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            b.ToCreateInstance(
                config => EndpointWithExternallyManagedContainer.Create(config, serviceCollection),
                (configured, ct) =>
                {
                    var serviceProvider = serviceCollection.BuildServiceProvider();

                    var transportAddressResolver = serviceProvider.GetRequiredService<ITransportAddressResolver>();

                    // HINT: Call before start
                    thrownException = Assert.Throws<Exception>(() => transportAddressResolver.ToTransportAddress(new QueueAddress("SomeAddress")));

                    return configured.Start(serviceProvider, ct);
                })
            )
            .Done(ctx => thrownException != null)
            .Run();

        StringAssert.Contains("Transport address resolution is not supported before the NServiceBus transport has been started.", thrownException.Message);
    }

    class Context : ScenarioContext
    {
        public bool Done { get; set; }
    }

    class ExternallyManagedContainerEndpoint : EndpointConfigurationBuilder
    {
        public ExternallyManagedContainerEndpoint() => EndpointSetup<DefaultServer>();
    }
}