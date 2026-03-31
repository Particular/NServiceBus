namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_resolving_address_translator : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_available_after_the_endpoint_is_started()
    {
        Context context = null;
        await Scenario.Define<Context>(c => context = c)
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b => b.CustomConfig(c => c.RegisterStartupTask<ExternallyManagedContainerEndpoint.StartupTaskAccessingResolver>()))
            .Run();

        Assert.That(context.TranslatedAddress, Is.Not.Null);
    }

    [Test]
    public void Should_throw_meaningful_exception_when_resolved_before_endpoint_started() =>
        Assert.That(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
                    b.ServiceResolve((provider, _) =>
                    {
                        var transportAddressResolver = provider.GetRequiredService<ITransportAddressResolver>();

                        transportAddressResolver.ToTransportAddress(new QueueAddress("SomeAddress"));
                        return Task.CompletedTask;
                    }))
                .Run();
        }, Throws.Exception.TypeOf<Exception>().With.Message.EqualTo("Transport address resolution is not supported before the NServiceBus transport has been started. Start the NServiceBus transport before calling `ToTransportAddress`"));

    class Context : ScenarioContext
    {
        public string TranslatedAddress { get; set; }
    }

    class ExternallyManagedContainerEndpoint : EndpointConfigurationBuilder
    {
        public ExternallyManagedContainerEndpoint() => EndpointSetup<DefaultServer>();

        internal class StartupTaskAccessingResolver(IServiceProvider provider, Context context) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                var transportAddressResolver = provider.GetRequiredService<ITransportAddressResolver>();

                context.TranslatedAddress = transportAddressResolver.ToTransportAddress(new QueueAddress("SomeAddress"));
                context.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}