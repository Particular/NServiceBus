namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_transport_is_started_send_only : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_provide_access_to_address_resolution()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.ResolvedAddress, Is.EqualTo("SomeAddress"));
    }

    class Context : ScenarioContext
    {
        public string ResolvedAddress { get; set; }
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.SendOnly();
                c.EnableFeature<FeatureAccessingAddressing>();
            });

        class FeatureAccessingAddressing : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                Assert.Throws<InvalidOperationException>(() => context.LocalQueueAddress(), "Should throw since the endpoint is send only");
                Assert.Throws<InvalidOperationException>(() => context.InstanceSpecificQueueAddress(), "Should throw since the endpoint is send only");

                context.RegisterStartupTask<StartupTask>();
            }
        }

        class StartupTask(Context testContext, ITransportAddressResolver resolver) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                testContext.ResolvedAddress = resolver.ToTransportAddress(new QueueAddress("SomeAddress"));
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}