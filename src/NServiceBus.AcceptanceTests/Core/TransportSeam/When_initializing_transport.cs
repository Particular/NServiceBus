namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;
    using Transport;

    public class When_initializing_transport : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_follow_the_startup_sequence()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            CollectionAssert.AreEqual(new List<string>
            {
                $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureReceiveInfrastructure)}",
                $"{nameof(ICreateQueues)}.{nameof(ICreateQueues.CreateQueueIfNecessary)}",
                $"{nameof(TransportReceiveInfrastructure)}.PreStartupCheck",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.Start)}",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureSendInfrastructure)}",
                $"{nameof(IPushMessages)}.{nameof(IPushMessages.Init)}",
                $"{nameof(TransportSendInfrastructure)}.PreStartupCheck",
                $"{nameof(IPushMessages)}.{nameof(IPushMessages.Start)}",
                $"{nameof(IPushMessages)}.{nameof(IPushMessages.Stop)}",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.Stop)}",
            }, context.StartUpSequence);
        }

        [Test]
        public async Task Should_follow_the_startup_sequence_for_send_only_endpoints()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.CustomConfig(c => c.SendOnly()))
                .Done(c => c.EndpointsStarted)
                .Run();

            CollectionAssert.AreEqual(new List<string>
            {
                $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.Start)}",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureSendInfrastructure)}",
                $"{nameof(TransportSendInfrastructure)}.PreStartupCheck",
                $"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.Stop)}",
            }, context.StartUpSequence);
        }

        class Context : ScenarioContext
        {
            public FakeTransport.StartUpSequence StartUpSequence { get; } = new FakeTransport.StartUpSequence();
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer, Context>((endpointConfig, context) =>
                {
                    endpointConfig.UseTransport<FakeTransport>()
                        .CollectStartupSequence(context.StartUpSequence);
                });
            }
        }
    }
}