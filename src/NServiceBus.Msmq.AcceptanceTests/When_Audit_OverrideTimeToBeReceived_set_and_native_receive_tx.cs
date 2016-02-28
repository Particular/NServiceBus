namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Audit_OverrideTimeToBeReceived_set_and_native_receive_tx : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Endpoint_should_not_start_and_show_error()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.Exceptions.First().Message.Contains("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ."));
        }

        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport<MsmqTransport>()
                            .Transactions(TransportTransactionMode.ReceiveOnly);
                })
                    .WithConfig<AuditConfig>(c => c.OverrideTimeToBeReceived = TimeSpan.FromHours(1));
            }
        }
    }
}