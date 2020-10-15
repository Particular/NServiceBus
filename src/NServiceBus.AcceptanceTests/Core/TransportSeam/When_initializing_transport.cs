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
                //$"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureSubscriptionInfrastructure)}",
                //$"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureSendInfrastructure)}",
                //$"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureReceiveInfrastructure)}",
                ////Completely removed.

                $"{nameof(ICreateQueues)}.{nameof(ICreateQueues.CreateQueueIfNecessary)}",
                $"{nameof(TransportSendInfrastructure)}.PreStartupCheck",
                $"{nameof(TransportReceiveInfrastructure)}.PreStartupCheck",
                //$"{nameof(IPushMessages)}.{nameof(IPushMessages.Init)}",
                $"{nameof(IPushMessages)}.{nameof(IPushMessages.Start)}",
                $"{nameof(IPushMessages)}.{nameof(IPushMessages.Stop)}",
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
                //$"{nameof(TransportInfrastructure)}.{nameof(TransportInfrastructure.ConfigureSendInfrastructure)}",
                $"{nameof(TransportSendInfrastructure)}.PreStartupCheck",
            }, context.StartUpSequence);
        }

        class Context : ScenarioContext
        {
            public List<string> StartUpSequence { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer, Context>((endpointConfig, context) =>
                {
                    var fakeTransport = new FakeTransport();
                    context.StartUpSequence = fakeTransport.StartUpSequence;
                    endpointConfig.UseTransport(fakeTransport);
                });
            }
        }
    }
}
