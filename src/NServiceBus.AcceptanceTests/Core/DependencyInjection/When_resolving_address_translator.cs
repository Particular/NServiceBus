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
        string translatedAddress = null;

        await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            b.ToCreateInstance(
                (services, config) => EndpointWithExternallyManagedContainer.Create(config, services),
                async (startableEndpoint, provider, ct) =>
                {
                    // HINT: Resolve before start
                    var transportAddressResolver = provider.GetRequiredService<ITransportAddressResolver>();

                    var endpoint = await startableEndpoint.Start(provider, ct);

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
        Exception thrownException = null;

        await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            b.ToCreateInstance(
                (services, config) => EndpointWithExternallyManagedContainer.Create(config, services),
                (startableEndpoint, provider, ct) =>
                {
                    var transportAddressResolver = provider.GetRequiredService<ITransportAddressResolver>();

                    // HINT: Call before start
                    thrownException = Assert.Throws<Exception>(() => transportAddressResolver.ToTransportAddress(new QueueAddress("SomeAddress")));

                    return startableEndpoint.Start(provider, ct);
                })
            )
            .Done(ctx => thrownException != null)
            .Run();

        Assert.That(thrownException.Message, Does.Contain("Transport address resolution is not supported before the NServiceBus transport has been started."));
    }

    class Context : ScenarioContext;

    class ExternallyManagedContainerEndpoint : EndpointConfigurationBuilder
    {
        public ExternallyManagedContainerEndpoint() => EndpointSetup<DefaultServer>();
    }
}