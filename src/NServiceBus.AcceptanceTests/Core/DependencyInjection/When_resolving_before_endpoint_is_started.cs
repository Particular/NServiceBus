namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_resolving_before_endpoint_is_started : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_able_to_use_transport_adddress_resolver_after_the_endpoint_is_started()
    {
        var serviceCollection = new ServiceCollection();
        string translatedAddress = null;

        var result = await Scenario.Define<Context>()
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
                })
                .When((session, ctx) =>
                {
                    ctx.Done = true;
                    return Task.CompletedTask;
                }))
            .Done(ctx => ctx.Done)
            .Run();

        Assert.That(translatedAddress, Is.Not.Null);
    }

    [Test]
    public async Task Attempt_to_resolve_transport_address_before_endpoint_is_started_throws_meaningful_exception()
    {
        var serviceCollection = new ServiceCollection();
        Exception thrownException = null;

        var result = await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            b.ToCreateInstance(
                config => EndpointWithExternallyManagedContainer.Create(config, serviceCollection),
                async (configured, ct) =>
                {
                    var serviceProvider = serviceCollection.BuildServiceProvider();

                    // HINT: Resolve before start
                    var transportAddressResolver = serviceProvider.GetRequiredService<ITransportAddressResolver>();

                    try
                    {
                        // HINT: Call before start
                        transportAddressResolver.ToTransportAddress(new QueueAddress("SomeAddress"));
                    }
                    catch (Exception ex)
                    {
                        thrownException = ex;
                    }

                    var endpoint = await configured.Start(serviceProvider, ct);

                    return endpoint;
                })
                .When((session, ctx) =>
                {
                    ctx.Done = true;
                    return Task.CompletedTask;
                }))
            .Done(ctx => ctx.Done)
            .Run();

        Assert.That(thrownException, Is.Not.Null);
        Assert.That(thrownException.Message.Contains("Transport address resolution is not supported before the NServiceBus transport has been started."), Is.True);

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
